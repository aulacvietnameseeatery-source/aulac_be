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
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;
    private readonly ILookupResolver _lookupResolver;
    private readonly AttendanceOptions _options;

    public ShiftAssignmentService(
        IShiftAssignmentRepository assignmentRepo,
        IShiftTemplateRepository templateRepo,
        IAccountRepository accountRepo,
        IUnitOfWork unitOfWork,
        INotificationService notificationService,
        ILookupResolver lookupResolver,
        IOptions<AttendanceOptions> options)
    {
        _assignmentRepo = assignmentRepo;
        _templateRepo = templateRepo;
        _accountRepo = accountRepo;
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _lookupResolver = lookupResolver;
        _options = options.Value;
    }

    public async Task<(List<ShiftAssignmentListDto> Items, int TotalCount)> GetAssignmentsAsync(
        GetShiftAssignmentRequest request, CancellationToken ct = default)
    {
        var (items, totalCount) = await _assignmentRepo.GetAssignmentsAsync(request, ct);
        return (items.Select(MapToListDto).ToList(), totalCount);
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

        var newStaff = await _accountRepo.FindByIdWithRoleAsync(request.NewStaffId, ct)
            ?? throw new NotFoundException($"New staff account {request.NewStaffId} not found");

        if (assignment.StaffId == request.NewStaffId)
            throw new ValidationException("New staff is the same as current staff");

        // Overlap check for new staff
        if (await _assignmentRepo.HasOverlappingAssignmentAsync(
            request.NewStaffId, assignment.WorkDate,
            assignment.PlannedStartAt, assignment.PlannedEndAt, ct: ct))
            throw new ConflictException(
                $"Staff ID {request.NewStaffId} has an overlapping shift during this time window");

        var oldStaffId = assignment.StaffId;
        assignment.StaffId = request.NewStaffId;
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
        return MapToDetailDto(updated);
    }

    public async Task<List<ShiftAssignmentListDto>> GetTeamScheduleAsync(
        TeamScheduleRequest request, CancellationToken ct = default)
    {
        var assignments = await _assignmentRepo.GetTeamScheduleAsync(
            request.WeekStart, request.ShiftTemplateId, ct);
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
}
