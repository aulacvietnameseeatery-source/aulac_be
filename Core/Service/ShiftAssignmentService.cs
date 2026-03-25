using Core.Data;
using Core.DTO.Notification;
using Core.DTO.Shift;
using Core.Entity;
using Core.Enum;
using Core.Exceptions;
using Core.Extensions;
using Core.Interface.Repo;
using Core.Interface.Service.Entity;
using Core.Interface.Service.Notification;
using Core.Interface.Service.Shift;
using Microsoft.Extensions.Options;

namespace Core.Service;

public class ShiftAssignmentService : IShiftAssignmentService
{
    private readonly IShiftAssignmentRepository _assignmentRepo;
    private readonly IShiftTemplateRepository _templateRepo;
    private readonly IAccountRepository _accountRepo;
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;
    private readonly ILookupResolver _lookupResolver;
    private readonly AttendanceOptions _options;
    private readonly IShiftLiveRealtimePublisher _shiftLiveRealtimePublisher;

    public ShiftAssignmentService(
        IShiftAssignmentRepository assignmentRepo,
        IShiftTemplateRepository templateRepo,
        IAccountRepository accountRepo,
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork,
        INotificationService notificationService,
        ILookupResolver lookupResolver,
        IOptions<AttendanceOptions> options,
        IShiftLiveRealtimePublisher shiftLiveRealtimePublisher)
    {
        _assignmentRepo = assignmentRepo;
        _templateRepo = templateRepo;
        _accountRepo = accountRepo;
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _lookupResolver = lookupResolver;
        _options = options.Value;
        _shiftLiveRealtimePublisher = shiftLiveRealtimePublisher;
    }

    public async Task<(List<ShiftAssignmentListDto> Items, int TotalCount)> GetAssignmentsAsync(
        GetShiftAssignmentRequest request, CancellationToken ct = default)
    {
        var (items, totalCount) = await _assignmentRepo.GetAssignmentsAsync(request, ct);
        return (items.Select(MapToListDto).ToList(), totalCount);
    }

    public async Task<List<ShiftLiveBoardItemDto>> GetLiveBoardAsync(
        GetShiftAssignmentRequest request, CancellationToken ct = default)
    {
        var liveRequest = new GetShiftAssignmentRequest
        {
            StaffId = request.StaffId,
            ShiftTemplateId = request.ShiftTemplateId,
            FromDate = request.FromDate,
            ToDate = request.ToDate,
            IsActive = request.IsActive,
            AssignmentStatusCode = request.AssignmentStatusCode,
            PageIndex = 1,
            PageSize = request.PageSize > 0 ? request.PageSize : 200,
        };

        var (items, _) = await _assignmentRepo.GetAssignmentsAsync(liveRequest, ct);
        if (items.Count == 0)
        {
            return new List<ShiftLiveBoardItemDto>();
        }

        var now = DateTime.UtcNow;
        var staffIds = items.Select(item => item.StaffId).Distinct().ToList();
        var fromDate = request.FromDate ?? items.Min(item => item.WorkDate);
        var toDate = request.ToDate ?? items.Max(item => item.WorkDate);
        var queryStart = fromDate.ToDateTime(TimeOnly.MinValue);
        var queryEnd = toDate.ToDateTime(TimeOnly.MaxValue);

        var orderSnapshots = await _orderRepository.GetShiftLiveOrderSnapshotsAsync(queryStart, queryEnd, staffIds, ct);
        var issueSnapshots = await _orderRepository.GetShiftLiveIssueSnapshotsAsync(queryStart, queryEnd, staffIds, ct);

        var ordersByStaff = orderSnapshots
            .GroupBy(snapshot => snapshot.StaffId)
            .ToDictionary(group => group.Key, group => (IReadOnlyCollection<ShiftLiveOrderSnapshotDto>)group.ToList());

        var issuesByStaff = issueSnapshots
            .GroupBy(snapshot => snapshot.StaffId)
            .ToDictionary(group => group.Key, group => (IReadOnlyCollection<ShiftLiveIssueSnapshotDto>)group.ToList());

        return items.Select(item => MapToLiveBoardDto(
            item,
            now,
            ordersByStaff.TryGetValue(item.StaffId, out var staffOrders) ? staffOrders : Array.Empty<ShiftLiveOrderSnapshotDto>(),
            issuesByStaff.TryGetValue(item.StaffId, out var staffIssues) ? staffIssues : Array.Empty<ShiftLiveIssueSnapshotDto>())).ToList();
    }

    public async Task<ShiftAssignmentDetailDto> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var assignment = await _assignmentRepo.GetByIdWithDetailsAsync(id, ct)
            ?? throw new NotFoundException("Shift assignment not found");
        return MapToDetailDto(assignment);
    }

    public async Task<ShiftAssignmentDetailDto> CreateAssignmentAsync(
        CreateShiftAssignmentRequest request, long assignedByStaffId, CancellationToken ct = default)
    {
        // Validate template
        var template = await _templateRepo.GetByIdAsync(request.ShiftTemplateId, ct)
            ?? throw new NotFoundException("Shift template not found");

        if (!template.IsActive)
            throw new ValidationException("Cannot assign to an inactive shift template");

        // Validate staff exists
        var staff = await _accountRepo.FindByIdWithRoleAsync(request.StaffId, ct)
            ?? throw new NotFoundException($"Staff account {request.StaffId} not found");

        // Auto-populate planned times from template defaults when not provided
        var plannedStart = request.PlannedStartAt
            ?? request.WorkDate.ToDateTime(template.DefaultStartTime);
        var plannedEnd = request.PlannedEndAt
            ?? request.WorkDate.ToDateTime(template.DefaultEndTime);

        if (plannedStart >= plannedEnd)
            throw new ValidationException("Planned start must be earlier than planned end");

        // Check if staff is already assigned to this template on this date
        var alreadyAssigned = await _assignmentRepo.GetAlreadyAssignedStaffIdsAsync(
            request.ShiftTemplateId, request.WorkDate, [request.StaffId], ct);

        if (alreadyAssigned.Count > 0)
            throw new ConflictException(
                $"Staff ID {request.StaffId} is already assigned to this shift template on {request.WorkDate}");

        // Check for overlapping shifts on the same day
        if (await _assignmentRepo.HasOverlappingAssignmentAsync(
            request.StaffId, request.WorkDate, plannedStart, plannedEnd, ct: ct))
            throw new ConflictException(
                $"Staff ID {request.StaffId} has an overlapping shift assignment during this time window");

        // Resolve assignment status: DRAFT | ASSIGNED
        var statusCode = request.IsDraft
            ? ShiftAssignmentStatusCode.DRAFT
            : ShiftAssignmentStatusCode.ASSIGNED;
        var statusLvId = await statusCode.ToShiftAssignmentStatusIdAsync(_lookupResolver, ct);

        var entity = new ShiftAssignment
        {
            ShiftTemplateId = request.ShiftTemplateId,
            StaffId = request.StaffId,
            WorkDate = request.WorkDate,
            PlannedStartAt = plannedStart,
            PlannedEndAt = plannedEnd,
            AssignmentStatusLvId = statusLvId,
            IsActive = true,
            Notes = request.Notes?.Trim(),
            Tags = request.Tags?.Trim(),
            AssignedBy = assignedByStaffId,
            AssignedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _assignmentRepo.Add(entity);
        await _unitOfWork.SaveChangesAsync(ct);

        // Only notify the assigned staff member when published (not DRAFT)
        if (!request.IsDraft)
        {
            await SendAssignmentNotificationAsync(entity, template, ct);
        }

        var saved = await _assignmentRepo.GetByIdWithDetailsAsync(entity.ShiftAssignmentId, ct)
            ?? throw new NotFoundException("Assignment not found after creation");
        await PublishShiftLiveUpdateAsync("assignment_created", entity.WorkDate, entity.ShiftAssignmentId, entity.StaffId, ct);
        return MapToDetailDto(saved);
    }

    public async Task<ShiftAssignmentDetailDto> UpdateAssignmentAsync(
        long id, UpdateShiftAssignmentRequest request, long updatedByStaffId, CancellationToken ct = default)
    {
        var assignment = await _assignmentRepo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Shift assignment not found");

        if (!assignment.IsActive)
            throw new ValidationException("Cannot edit a cancelled assignment");

        // Guard: cannot edit after check-in
        if (assignment.AttendanceRecord?.ActualCheckInAt is not null)
            throw new ConflictException("Cannot edit an assignment after staff has checked in");

        var start = request.PlannedStartAt ?? assignment.PlannedStartAt;
        var end = request.PlannedEndAt ?? assignment.PlannedEndAt;

        if (start >= end)
            throw new ValidationException("Planned start must be earlier than planned end");

        if (request.PlannedStartAt.HasValue || request.PlannedEndAt.HasValue)
        {
            if (await _assignmentRepo.HasOverlappingAssignmentAsync(
                assignment.StaffId, assignment.WorkDate, start, end, excludeAssignmentId: id, ct: ct))
                throw new ConflictException("Updated time window overlaps with another active assignment");

            assignment.PlannedStartAt = start;
            assignment.PlannedEndAt = end;
        }

        if (request.Notes is not null)
            assignment.Notes = request.Notes.Trim();

        if (request.Tags is not null)
            assignment.Tags = request.Tags.Trim();

        assignment.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(ct);

        var updated = await _assignmentRepo.GetByIdWithDetailsAsync(id, ct)
            ?? throw new NotFoundException("Assignment not found after update");
        await PublishShiftLiveUpdateAsync("assignment_updated", updated.WorkDate, updated.ShiftAssignmentId, updated.StaffId, ct);
        return MapToDetailDto(updated);
    }

    public async Task CancelAssignmentAsync(long id, CancellationToken ct = default)
    {
        var assignment = await _assignmentRepo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Shift assignment not found");

        if (!assignment.IsActive)
            throw new ValidationException("Assignment is already cancelled");

        if (assignment.AttendanceRecord?.ActualCheckInAt is not null)
            throw new ConflictException("Cannot cancel assignment after staff has checked in");

        var cancelledStatusId = await ShiftAssignmentStatusCode.CANCELLED
            .ToShiftAssignmentStatusIdAsync(_lookupResolver, ct);

        assignment.IsActive = false;
        assignment.AssignmentStatusLvId = cancelledStatusId;
        assignment.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(ct);
        await PublishShiftLiveUpdateAsync("assignment_cancelled", assignment.WorkDate, assignment.ShiftAssignmentId, assignment.StaffId, ct);
    }
    public async Task<(List<ShiftAssignmentDetailDto> Items, int TotalCount)> GetMyShiftsAsync(
        long staffId, GetShiftAssignmentRequest request, CancellationToken ct = default)
    {
        // Force the staff filter to the authenticated staff member
        request.StaffId = staffId;

        var (items, totalCount) = await _assignmentRepo.GetAssignmentsAsync(request, ct);
        return (items.Select(MapToDetailDto).ToList(), totalCount);
    }

    // Mapping helpers

    private async Task SendAssignmentNotificationAsync(
        ShiftAssignment entity, ShiftTemplate template, CancellationToken ct)
    {
        await _notificationService.PublishAsync(new PublishNotificationRequest
        {
            Type = nameof(NotificationType.SHIFT_ASSIGNED),
            Title = "Shift Assigned",
            Body = $"You have been assigned to {template.TemplateName} on {entity.WorkDate:yyyy-MM-dd}",
            Priority = nameof(NotificationPriority.Normal),
            SoundKey = "notification_normal",
            ActionUrl = "/dashboard/my-shifts",
            EntityType = "ShiftAssignment",
            EntityId = entity.ShiftAssignmentId.ToString(),
            Metadata = new Dictionary<string, object>
            {
                ["shiftAssignmentId"] = entity.ShiftAssignmentId.ToString(),
                ["templateName"] = template.TemplateName,
                ["workDate"] = entity.WorkDate.ToString("yyyy-MM-dd"),
                ["startTime"] = entity.PlannedStartAt.ToString("HH:mm"),
                ["endTime"] = entity.PlannedEndAt.ToString("HH:mm")
            },
            TargetUserIds = new List<long> { entity.StaffId }
        }, ct);
    }

    public async Task<List<ShiftAssignmentDetailDto>> BulkCreateAssignmentsAsync(
        BulkCreateAssignmentRequest request, long assignedByStaffId, CancellationToken ct = default)
    {
        var template = await _templateRepo.GetByIdAsync(request.ShiftTemplateId, ct)
            ?? throw new NotFoundException("Shift template not found");

        if (!template.IsActive)
            throw new ValidationException("Cannot assign to an inactive shift template");

        var plannedStart = request.PlannedStartAt
            ?? request.WorkDate.ToDateTime(template.DefaultStartTime);
        var plannedEnd = request.PlannedEndAt
            ?? request.WorkDate.ToDateTime(template.DefaultEndTime);

        if (plannedStart >= plannedEnd)
            throw new ValidationException("Planned start must be earlier than planned end");

        // Filter out already-assigned staff
        var alreadyAssigned = await _assignmentRepo.GetAlreadyAssignedStaffIdsAsync(
            request.ShiftTemplateId, request.WorkDate, request.StaffIds, ct);
        var validStaffIds = request.StaffIds.Except(alreadyAssigned).Distinct().ToList();

        if (validStaffIds.Count == 0)
            throw new ConflictException("All selected staff are already assigned to this shift on the given date");

        var statusCode = request.IsDraft
            ? ShiftAssignmentStatusCode.DRAFT
            : ShiftAssignmentStatusCode.ASSIGNED;
        var statusLvId = await statusCode.ToShiftAssignmentStatusIdAsync(_lookupResolver, ct);

        var now = DateTime.UtcNow;
        var entities = new List<ShiftAssignment>();

        foreach (var staffId in validStaffIds)
        {
            // Validate staff exists
            _ = await _accountRepo.FindByIdWithRoleAsync(staffId, ct)
                ?? throw new NotFoundException($"Staff account {staffId} not found");

            // Skip if overlapping (soft skip — don't fail the whole batch)
            if (await _assignmentRepo.HasOverlappingAssignmentAsync(
                staffId, request.WorkDate, plannedStart, plannedEnd, ct: ct))
                continue;

            entities.Add(new ShiftAssignment
            {
                ShiftTemplateId = request.ShiftTemplateId,
                StaffId = staffId,
                WorkDate = request.WorkDate,
                PlannedStartAt = plannedStart,
                PlannedEndAt = plannedEnd,
                AssignmentStatusLvId = statusLvId,
                IsActive = true,
                Notes = request.Notes?.Trim(),
                Tags = request.Tags?.Trim(),
                AssignedBy = assignedByStaffId,
                AssignedAt = now,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        if (entities.Count == 0)
            throw new ConflictException("No assignments could be created — all staff have overlapping shifts");

        _assignmentRepo.AddRange(entities);
        await _unitOfWork.SaveChangesAsync(ct);

        // Notify each assigned staff (not for DRAFTs)
        if (!request.IsDraft)
        {
            foreach (var entity in entities)
                await SendAssignmentNotificationAsync(entity, template, ct);
        }

        // Reload with full details
        var ids = entities.Select(e => e.ShiftAssignmentId).ToList();
        var saved = await _assignmentRepo.GetByIdsWithDetailsAsync(ids, ct);
        await PublishShiftLiveUpdateAsync("assignments_bulk_created", request.WorkDate, null, null, ct);
        return saved.Select(MapToDetailDto).ToList();
    }

    public async Task<List<ShiftAssignmentListDto>> PublishAssignmentsAsync(
        PublishAssignmentsRequest request, long publishedByStaffId, CancellationToken ct = default)
    {
        List<ShiftAssignment> drafts;

        if (request.AssignmentIds is { Count: > 0 })
        {
            drafts = await _assignmentRepo.GetByIdsWithDetailsAsync(request.AssignmentIds, ct);
            // Only publish actual DRAFTs
            var draftStatusId = await ShiftAssignmentStatusCode.DRAFT
                .ToShiftAssignmentStatusIdAsync(_lookupResolver, ct);
            drafts = drafts.Where(a => a.AssignmentStatusLvId == draftStatusId && a.IsActive).ToList();
        }
        else if (request.FromDate.HasValue && request.ToDate.HasValue)
        {
            drafts = await _assignmentRepo.GetDraftAssignmentsAsync(
                request.FromDate.Value, request.ToDate.Value, ct);
        }
        else
        {
            throw new ValidationException(
                "Either AssignmentIds or both FromDate and ToDate must be provided");
        }

        if (drafts.Count == 0)
            return new List<ShiftAssignmentListDto>();

        var assignedStatusId = await ShiftAssignmentStatusCode.ASSIGNED
            .ToShiftAssignmentStatusIdAsync(_lookupResolver, ct);
        var now = DateTime.UtcNow;

        foreach (var draft in drafts)
        {
            draft.AssignmentStatusLvId = assignedStatusId;
            draft.UpdatedAt = now;
        }

        await _unitOfWork.SaveChangesAsync(ct);

        // Send notifications
        foreach (var draft in drafts)
        {
            if (draft.ShiftTemplate is not null)
                await SendAssignmentNotificationAsync(draft, draft.ShiftTemplate, ct);
        }

        await PublishShiftLiveUpdateAsync("assignments_published", request.FromDate ?? drafts.Min(d => d.WorkDate), null, null, ct);

        return drafts.Select(MapToListDto).ToList();
    }

    public async Task<List<ShiftAssignmentListDto>> CopyWeekAsync(
        CopyWeekRequest request, long assignedByStaffId, CancellationToken ct = default)
    {
        if (request.SourceWeekStart.DayOfWeek != DayOfWeek.Monday)
            throw new ValidationException("SourceWeekStart must be a Monday");
        if (request.TargetWeekStart.DayOfWeek != DayOfWeek.Monday)
            throw new ValidationException("TargetWeekStart must be a Monday");

        var sourceAssignments = await _assignmentRepo.GetWeekAssignmentsAsync(
            request.SourceWeekStart, ct);

        if (sourceAssignments.Count == 0)
            throw new NotFoundException("No active assignments found in the source week");

        var dayOffset = request.TargetWeekStart.DayNumber - request.SourceWeekStart.DayNumber;
        var statusCode = request.AsDraft
            ? ShiftAssignmentStatusCode.DRAFT
            : ShiftAssignmentStatusCode.ASSIGNED;
        var statusLvId = await statusCode.ToShiftAssignmentStatusIdAsync(_lookupResolver, ct);
        var now = DateTime.UtcNow;

        var newEntities = new List<ShiftAssignment>();

        foreach (var src in sourceAssignments)
        {
            var targetDate = src.WorkDate.AddDays(dayOffset);

            // Skip if already assigned to same template+staff+date
            var already = await _assignmentRepo.GetAlreadyAssignedStaffIdsAsync(
                src.ShiftTemplateId, targetDate, [src.StaffId], ct);
            if (already.Count > 0) continue;

            var newStart = src.PlannedStartAt.AddDays(dayOffset);
            var newEnd = src.PlannedEndAt.AddDays(dayOffset);

            if (await _assignmentRepo.HasOverlappingAssignmentAsync(
                src.StaffId, targetDate, newStart, newEnd, ct: ct))
                continue;

            newEntities.Add(new ShiftAssignment
            {
                ShiftTemplateId = src.ShiftTemplateId,
                StaffId = src.StaffId,
                WorkDate = targetDate,
                PlannedStartAt = newStart,
                PlannedEndAt = newEnd,
                AssignmentStatusLvId = statusLvId,
                IsActive = true,
                Notes = src.Notes,
                Tags = src.Tags,
                AssignedBy = assignedByStaffId,
                AssignedAt = now,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        if (newEntities.Count == 0)
            throw new ConflictException("No assignments could be copied — all slots are already filled or overlap");

        _assignmentRepo.AddRange(newEntities);
        await _unitOfWork.SaveChangesAsync(ct);

        var ids = newEntities.Select(e => e.ShiftAssignmentId).ToList();
        var saved = await _assignmentRepo.GetByIdsWithDetailsAsync(ids, ct);
        await PublishShiftLiveUpdateAsync("assignments_copied", request.TargetWeekStart, null, null, ct);
        return saved.Select(MapToListDto).ToList();
    }

    public async Task<ShiftAssignmentDetailDto> ReassignAsync(
        long assignmentId, ReassignRequest request, long reassignedByStaffId, CancellationToken ct = default)
    {
        var assignment = await _assignmentRepo.GetByIdWithDetailsAsync(assignmentId, ct)
            ?? throw new NotFoundException("Shift assignment not found");

        if (!assignment.IsActive)
            throw new ValidationException("Cannot reassign a cancelled assignment");

        if (assignment.AttendanceRecord?.ActualCheckInAt is not null)
            throw new ConflictException("Cannot reassign after staff has checked in");

        var targetStaffId = request.NewStaffId;
        var targetWorkDate = request.NewWorkDate ?? assignment.WorkDate;

        var newStaff = await _accountRepo.FindByIdWithRoleAsync(targetStaffId, ct)
            ?? throw new NotFoundException($"New staff account {request.NewStaffId} not found");

        if (assignment.StaffId == targetStaffId && assignment.WorkDate == targetWorkDate)
            throw new ValidationException("Assignment already belongs to this staff on this date");

        // Overlap check for target staff/date
        if (await _assignmentRepo.HasOverlappingAssignmentAsync(
            targetStaffId, targetWorkDate,
            assignment.PlannedStartAt, assignment.PlannedEndAt,
            excludeAssignmentId: assignmentId,
            ct: ct))
            throw new ConflictException(
                $"Staff ID {targetStaffId} has an overlapping shift during this time window");

        var oldStaffId = assignment.StaffId;
        assignment.StaffId = targetStaffId;
        assignment.WorkDate = targetWorkDate;
        assignment.PlannedStartAt = targetWorkDate.ToDateTime(TimeOnly.FromDateTime(assignment.PlannedStartAt));
        assignment.PlannedEndAt = targetWorkDate.ToDateTime(TimeOnly.FromDateTime(assignment.PlannedEndAt));
        assignment.Notes = string.IsNullOrWhiteSpace(request.Reason)
            ? assignment.Notes
            : $"{assignment.Notes}\n[Reassigned] {request.Reason}".Trim();
        assignment.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(ct);

        // Notify new staff
        if (assignment.ShiftTemplate is not null)
            await SendAssignmentNotificationAsync(assignment, assignment.ShiftTemplate, ct);

        var updated = await _assignmentRepo.GetByIdWithDetailsAsync(assignmentId, ct)
            ?? throw new NotFoundException("Assignment not found after reassign");
        await PublishShiftLiveUpdateAsync("assignment_reassigned", updated.WorkDate, updated.ShiftAssignmentId, updated.StaffId, ct);
        return MapToDetailDto(updated);
    }

    public async Task<ShiftAssignmentDetailDto> ConfirmAssignmentAsync(
        long assignmentId, long staffId, CancellationToken ct = default)
    {
        var assignment = await _assignmentRepo.GetByIdWithDetailsAsync(assignmentId, ct)
            ?? throw new NotFoundException("Shift assignment not found");

        if (assignment.StaffId != staffId)
            throw new ForbiddenException("You can only confirm your own assigned shifts");

        var assignedStatusId = await ShiftAssignmentStatusCode.ASSIGNED
            .ToShiftAssignmentStatusIdAsync(_lookupResolver, ct);

        if (assignment.AssignmentStatusLvId != assignedStatusId)
            throw new ValidationException("Only ASSIGNED shifts can be confirmed");

        var confirmedStatusId = await ShiftAssignmentStatusCode.CONFIRMED
            .ToShiftAssignmentStatusIdAsync(_lookupResolver, ct);

        assignment.AssignmentStatusLvId = confirmedStatusId;
        assignment.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(ct);

        var updated = await _assignmentRepo.GetByIdWithDetailsAsync(assignmentId, ct)
            ?? throw new NotFoundException("Assignment not found after confirm");
        await PublishShiftLiveUpdateAsync("assignment_confirmed", updated.WorkDate, updated.ShiftAssignmentId, updated.StaffId, ct);
        return MapToDetailDto(updated);
    }

    public async Task<List<ShiftAssignmentListDto>> GetTeamScheduleAsync(
        TeamScheduleRequest request, CancellationToken ct = default)
    {
        var weekEnd = request.WeekEnd ?? request.WeekStart.AddDays(6);
        if (weekEnd < request.WeekStart)
            throw new ValidationException("WeekEnd must be on or after WeekStart");

        var assignments = await _assignmentRepo.GetTeamScheduleAsync(
            request.WeekStart, weekEnd, request.ShiftTemplateId, ct);
        return assignments.Select(MapToListDto).ToList();
    }

    internal static ShiftAssignmentListDto MapToListDto(ShiftAssignment a) => new()
    {
        ShiftAssignmentId = a.ShiftAssignmentId,
        ShiftTemplateId = a.ShiftTemplateId,
        TemplateName = a.ShiftTemplate?.TemplateName ?? "Unknown",
        StaffId = a.StaffId,
        StaffName = a.Staff?.FullName ?? "Unknown",
        WorkDate = a.WorkDate,
        PlannedStartAt = a.PlannedStartAt,
        PlannedEndAt = a.PlannedEndAt,
        AssignmentStatusCode = a.AssignmentStatusLv?.ValueCode ?? "UNKNOWN",
        AssignmentStatusName = a.AssignmentStatusLv?.ValueName ?? "Unknown",
        IsActive = a.IsActive,
        Tags = a.Tags,
        Notes = a.Notes,
        AssignedAt = a.AssignedAt,
        AssignedByName = a.AssignedByStaff?.FullName ?? "Unknown"
    };

    internal static ShiftAssignmentDetailDto MapToDetailDto(ShiftAssignment a) => new()
    {
        ShiftAssignmentId = a.ShiftAssignmentId,
        ShiftTemplateId = a.ShiftTemplateId,
        TemplateName = a.ShiftTemplate?.TemplateName ?? "Unknown",
        StaffId = a.StaffId,
        StaffName = a.Staff?.FullName ?? "Unknown",
        WorkDate = a.WorkDate,
        PlannedStartAt = a.PlannedStartAt,
        PlannedEndAt = a.PlannedEndAt,
        AssignmentStatusCode = a.AssignmentStatusLv?.ValueCode ?? "UNKNOWN",
        AssignmentStatusName = a.AssignmentStatusLv?.ValueName ?? "Unknown",
        IsActive = a.IsActive,
        Tags = a.Tags,
        Notes = a.Notes,
        AssignedAt = a.AssignedAt,
        AssignedByName = a.AssignedByStaff?.FullName ?? "Unknown",
        Attendance = a.AttendanceRecord is not null ? MapAttendance(a.AttendanceRecord) : null
    };

    internal static AttendanceRecordDto MapAttendance(AttendanceRecord ar) => new()
    {
        AttendanceId = ar.AttendanceId,
        ShiftAssignmentId = ar.ShiftAssignmentId,
        AttendanceStatusCode = ar.AttendanceStatusLv?.ValueCode ?? "UNKNOWN",
        AttendanceStatusName = ar.AttendanceStatusLv?.ValueName ?? "Unknown",
        ActualCheckInAt = ar.ActualCheckInAt,
        ActualCheckOutAt = ar.ActualCheckOutAt,
        LateMinutes = ar.LateMinutes,
        EarlyLeaveMinutes = ar.EarlyLeaveMinutes,
        WorkedMinutes = ar.WorkedMinutes,
        IsManualAdjustment = ar.IsManualAdjustment,
        AdjustmentReason = ar.AdjustmentReason,
        ReviewedByName = ar.ReviewedByStaff?.FullName,
        ReviewedAt = ar.ReviewedAt,
        TimeLogs = ar.TimeLogs?.Select(MapTimeLog).ToList() ?? new()
    };

    internal static TimeLogDto MapTimeLog(TimeLog tl) => new()
    {
        TimeLogId = tl.TimeLogId,
        PunchInTime = tl.PunchInTime,
        PunchOutTime = tl.PunchOutTime,
        ValidationStatus = tl.ValidationStatus,
        PunchDurationMinutes = tl.PunchDurationMinutes,
        CreatedAt = tl.CreatedAt
    };

    internal static ShiftLiveBoardItemDto MapToLiveBoardDto(
        ShiftAssignment assignment,
        DateTime nowUtc,
        IReadOnlyCollection<ShiftLiveOrderSnapshotDto> orderSnapshots,
        IReadOnlyCollection<ShiftLiveIssueSnapshotDto> issueSnapshots)
    {
        var attendance = assignment.AttendanceRecord;
        var attendanceStatusCode = attendance?.AttendanceStatusLv?.ValueCode;
        var attendanceStatusName = attendance?.AttendanceStatusLv?.ValueName;
        var effectiveWindowStart = attendance?.ActualCheckInAt ?? assignment.PlannedStartAt;
        var effectiveWindowEnd = attendance?.ActualCheckOutAt
            ?? (nowUtc < assignment.PlannedEndAt ? nowUtc : assignment.PlannedEndAt);

        var relevantOrders = orderSnapshots
            .Where(snapshot => IsWithinShiftWindow(snapshot.CreatedAt ?? snapshot.LastActivityAt, effectiveWindowStart, effectiveWindowEnd)
                || IsWithinShiftWindow(snapshot.LatestPaidAt, effectiveWindowStart, effectiveWindowEnd))
            .OrderByDescending(snapshot => snapshot.LastActivityAt ?? snapshot.UpdatedAt ?? snapshot.CreatedAt)
            .ToList();

        var latestOrder = relevantOrders.FirstOrDefault();
        var ordersHandledCount = relevantOrders.Count(snapshot => IsWithinShiftWindow(snapshot.CreatedAt, effectiveWindowStart, effectiveWindowEnd));
        var paidOrders = relevantOrders
            .Where(snapshot => IsWithinShiftWindow(snapshot.LatestPaidAt, effectiveWindowStart, effectiveWindowEnd))
            .ToList();

        var unresolvedIssues = issueSnapshots
            .Where(snapshot => !snapshot.IsResolved && IsWithinShiftWindow(snapshot.CreatedAt, effectiveWindowStart, effectiveWindowEnd))
            .OrderByDescending(snapshot => snapshot.CreatedAt)
            .ToList();

        var liveStatusCode = ResolveLiveStatusCode(assignment, attendance, nowUtc);
        var liveStatusName = liveStatusCode switch
        {
            "ON_DUTY" => "On Duty",
            "WAITING" => "Waiting For Shift",
            "COMPLETED" => "Completed",
            "NOT_CHECKED_IN" => "Needs Attention",
            _ => "Unknown"
        };

        var hasAlert = attendance?.LateMinutes > 0
            || string.Equals(attendanceStatusCode, nameof(AttendanceStatusCode.ABSENT), StringComparison.OrdinalIgnoreCase)
            || string.Equals(attendanceStatusCode, nameof(AttendanceStatusCode.EARLY_LEAVE), StringComparison.OrdinalIgnoreCase)
            || liveStatusCode == "NOT_CHECKED_IN"
            || unresolvedIssues.Count > 0;

        var roleCode = assignment.Staff?.Role?.RoleCode ?? string.Empty;
        var roleName = assignment.Staff?.Role?.RoleName ?? "Unknown";
        var currentTaskLabel = BuildCurrentTaskLabel(roleCode, roleName, latestOrder?.TableCode, ordersHandledCount, paidOrders.Count, relevantOrders.Sum(snapshot => snapshot.PendingItemsCount));
        var currentLocationLabel = BuildCurrentLocationLabel(roleCode, roleName, latestOrder?.TableCode);

        return new ShiftLiveBoardItemDto
        {
            ShiftAssignmentId = assignment.ShiftAssignmentId,
            ShiftTemplateId = assignment.ShiftTemplateId,
            TemplateName = assignment.ShiftTemplate?.TemplateName ?? "Unknown",
            StaffId = assignment.StaffId,
            StaffName = assignment.Staff?.FullName ?? "Unknown",
            StaffRoleCode = roleCode,
            StaffRoleName = roleName,
            WorkDate = assignment.WorkDate,
            PlannedStartAt = assignment.PlannedStartAt,
            PlannedEndAt = assignment.PlannedEndAt,
            AssignmentStatusCode = assignment.AssignmentStatusLv?.ValueCode ?? "UNKNOWN",
            AssignmentStatusName = assignment.AssignmentStatusLv?.ValueName ?? "Unknown",
            IsActive = assignment.IsActive,
            Tags = assignment.Tags,
            Notes = assignment.Notes,
            AssignedAt = assignment.AssignedAt,
            AssignedByName = assignment.AssignedByStaff?.FullName ?? "Unknown",
            AttendanceStatusCode = attendanceStatusCode,
            AttendanceStatusName = attendanceStatusName,
            ActualCheckInAt = attendance?.ActualCheckInAt,
            ActualCheckOutAt = attendance?.ActualCheckOutAt,
            LateMinutes = attendance?.LateMinutes ?? 0,
            EarlyLeaveMinutes = attendance?.EarlyLeaveMinutes ?? 0,
            WorkedMinutes = attendance?.WorkedMinutes ?? 0,
            IsManualAdjustment = attendance?.IsManualAdjustment ?? false,
            LiveStatusCode = liveStatusCode,
            LiveStatusName = liveStatusName,
            HasAlert = hasAlert,
            CurrentTaskLabel = currentTaskLabel,
            CurrentLocationLabel = currentLocationLabel,
            OrdersHandledCount = ordersHandledCount,
            PaidBillsCount = paidOrders.Count,
            CurrentRevenue = paidOrders.Sum(snapshot => snapshot.PaidRevenue),
            ItemsCompletedCount = relevantOrders.Sum(snapshot => snapshot.CompletedItemsCount),
            PendingTicketsCount = relevantOrders.Sum(snapshot => snapshot.PendingItemsCount),
            IssueCount = unresolvedIssues.Count,
            LatestIssueText = unresolvedIssues.FirstOrDefault()?.Description,
        };
    }

    private static bool IsWithinShiftWindow(DateTime? timestamp, DateTime windowStart, DateTime windowEnd)
    {
        return timestamp.HasValue && timestamp.Value >= windowStart && timestamp.Value <= windowEnd;
    }

    private static string? BuildCurrentTaskLabel(
        string roleCode,
        string roleName,
        string? tableCode,
        int ordersHandledCount,
        int paidBillsCount,
        int pendingTicketsCount)
    {
        var normalized = string.IsNullOrWhiteSpace(roleCode) ? roleName : roleCode;
        normalized = normalized.Trim().ToUpperInvariant();

        if ((normalized.Contains("SERVER") || normalized.Contains("WAITER") || normalized.Contains("PHUC")) && !string.IsNullOrWhiteSpace(tableCode))
            return $"Dang phuc vu ban {tableCode}";

        if ((normalized.Contains("CASH") || normalized.Contains("THU")) && paidBillsCount > 0)
            return $"Da xu ly {paidBillsCount} bill trong ca";

        if ((normalized.Contains("BAR") || normalized.Contains("BARTENDER") || normalized.Contains("PHA")) && pendingTicketsCount > 0)
            return $"Dang xu ly {pendingTicketsCount} mon dang cho";

        if (ordersHandledCount > 0)
            return $"Da tiep nhan {ordersHandledCount} order trong ca";

        return null;
    }

    private static string? BuildCurrentLocationLabel(string roleCode, string roleName, string? tableCode)
    {
        var normalized = string.IsNullOrWhiteSpace(roleCode) ? roleName : roleCode;
        normalized = normalized.Trim().ToUpperInvariant();

        if ((normalized.Contains("SERVER") || normalized.Contains("WAITER") || normalized.Contains("PHUC")) && !string.IsNullOrWhiteSpace(tableCode))
            return $"Tai khu vuc ban {tableCode}";

        if (normalized.Contains("CASH") || normalized.Contains("THU"))
            return "Dang tai quay POS";

        if (normalized.Contains("BAR") || normalized.Contains("BARTENDER") || normalized.Contains("PHA"))
            return "Dang tai quay Bar";

        return null;
    }

    private Task PublishShiftLiveUpdateAsync(
        string eventType,
        DateOnly workDate,
        long? shiftAssignmentId,
        long? staffId,
        CancellationToken ct)
    {
        return _shiftLiveRealtimePublisher.PublishBoardChangedAsync(new ShiftLiveRealtimeEventDto
        {
            EventType = eventType,
            WorkDate = workDate,
            ShiftAssignmentId = shiftAssignmentId,
            StaffId = staffId,
            OccurredAt = DateTime.UtcNow,
        }, ct);
    }

    private static string ResolveLiveStatusCode(
        ShiftAssignment assignment,
        AttendanceRecord? attendance,
        DateTime nowUtc)
    {
        if (!assignment.IsActive)
            return "COMPLETED";

        if (attendance?.ActualCheckOutAt is not null)
            return "COMPLETED";

        if (attendance?.ActualCheckInAt is not null)
            return "ON_DUTY";

        if (nowUtc < assignment.PlannedStartAt)
            return "WAITING";

        if (string.Equals(attendance?.AttendanceStatusLv?.ValueCode, nameof(AttendanceStatusCode.ABSENT), StringComparison.OrdinalIgnoreCase))
            return "NOT_CHECKED_IN";

        if (nowUtc <= assignment.PlannedEndAt)
            return "NOT_CHECKED_IN";

        return "NOT_CHECKED_IN";
    }
}
