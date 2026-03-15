using Core.DTO.Shift;
using Core.Entity;
using Core.Exceptions;
using Core.Interface.Repo;
using Core.Interface.Service.Shift;

namespace Core.Service;

public class ShiftAssignmentService : IShiftAssignmentService
{
    private readonly IShiftAssignmentRepository _assignmentRepo;
    private readonly IShiftTemplateRepository _templateRepo;
    private readonly IAccountRepository _accountRepo;
    private readonly IUnitOfWork _unitOfWork;

    public ShiftAssignmentService(
        IShiftAssignmentRepository assignmentRepo,
        IShiftTemplateRepository templateRepo,
        IAccountRepository accountRepo,
        IUnitOfWork unitOfWork)
    {
        _assignmentRepo = assignmentRepo;
        _templateRepo = templateRepo;
        _accountRepo = accountRepo;
        _unitOfWork = unitOfWork;
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

        var entity = new ShiftAssignment
        {
            ShiftTemplateId = request.ShiftTemplateId,
            StaffId = request.StaffId,
            WorkDate = request.WorkDate,
            PlannedStartAt = plannedStart,
            PlannedEndAt = plannedEnd,
            IsActive = true,
            Notes = request.Notes?.Trim(),
            AssignedBy = assignedByStaffId,
            AssignedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _assignmentRepo.Add(entity);
        await _unitOfWork.SaveChangesAsync(ct);

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

        assignment.IsActive = false;
        assignment.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(ct);
    }

    // Mapping helpers

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
        IsActive = a.IsActive,
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
        IsActive = a.IsActive,
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
        ReviewedAt = ar.ReviewedAt
    };
}
