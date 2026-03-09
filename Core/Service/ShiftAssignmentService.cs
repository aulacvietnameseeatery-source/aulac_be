using Core.DTO.Shift;
using Core.Entity;
using Core.Exceptions;
using Core.Extensions;
using Core.Interface.Repo;
using Core.Interface.Service.Entity;
using Core.Interface.Service.Shift;
using LookupType = Core.Enum.LookupType;

namespace Core.Service;

public class ShiftAssignmentService : IShiftAssignmentService
{
    private readonly IShiftAssignmentRepository _assignmentRepo;
    private readonly IShiftScheduleRepository _scheduleRepo;
    private readonly IAccountRepository _accountRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILookupResolver _lookupResolver;

    public ShiftAssignmentService(
        IShiftAssignmentRepository assignmentRepo,
        IShiftScheduleRepository scheduleRepo,
        IAccountRepository accountRepo,
        IUnitOfWork unitOfWork,
        ILookupResolver lookupResolver)
    {
        _assignmentRepo = assignmentRepo;
        _scheduleRepo = scheduleRepo;
        _accountRepo = accountRepo;
        _unitOfWork = unitOfWork;
        _lookupResolver = lookupResolver;
    }

    public async Task<(List<ShiftAssignmentDto> Items, int TotalCount)> GetAssignmentsAsync(
        GetShiftAssignmentRequest request, CancellationToken ct = default)
    {
        var (items, totalCount) = await _assignmentRepo.GetAssignmentsAsync(request, ct);
        return (items.Select(ShiftScheduleService.MapAssignment).ToList(), totalCount);
    }

    public async Task<List<ShiftAssignmentDto>> CreateAssignmentsAsync(
           CreateShiftAssignmentRequest request, long assignedByStaffId, CancellationToken ct = default)
    {
        // Validate schedule
        var schedule = await _scheduleRepo.GetByIdAsync(request.ShiftScheduleId, ct)
                        ?? throw new NotFoundException("Shift schedule not found");

        var scheduleStatusCode = schedule.StatusLv?.ValueCode ?? "UNKNOWN";
        if (scheduleStatusCode is nameof(ShiftStatusCode.CLOSED) or nameof(ShiftStatusCode.CANCELLED))
            throw new ValidationException($"Cannot assign staff to a schedule with status '{scheduleStatusCode}'");

        // ?? Check for duplicates within the schedule ??
        var alreadyAssigned = await _assignmentRepo.GetAlreadyAssignedStaffIdsAsync(
        request.ShiftScheduleId, request.StaffIds, ct);

        if (alreadyAssigned.Count > 0)
            throw new ConflictException(
                $"Staff ID(s) {string.Join(", ", alreadyAssigned)} are already assigned to this schedule");

        // ?? Check overlapping shifts per staff ??
        foreach (var staffId in request.StaffIds)
        {
            if (await _assignmentRepo.HasOverlappingAssignmentAsync(staffId, schedule.PlannedStartAt, schedule.PlannedEndAt, ct: ct))
                throw new ConflictException($"Staff ID {staffId} has an overlapping active shift assignment during this time window");
        }

        var assignedStatusId = await ShiftAssignmentStatusCode.ASSIGNED.ToShiftAssignmentStatusIdAsync(_lookupResolver, ct);

        var entities = new List<ShiftAssignment>();
        foreach (var staffId in request.StaffIds)
        {
            var staff = await _accountRepo.FindByIdWithRoleAsync(staffId, ct)
                            ?? throw new NotFoundException($"Staff account {staffId} not found");

            entities.Add(new ShiftAssignment
            {
                ShiftScheduleId = request.ShiftScheduleId,
                StaffId = staffId,
                RoleId = staff.RoleId,
                AssignmentStatusLvId = assignedStatusId,
                AssignedBy = assignedByStaffId,
                AssignedAt = DateTime.UtcNow,
                Remarks = request.Remarks
            });
        }

        _assignmentRepo.AddRange(entities);
        await _unitOfWork.SaveChangesAsync(ct);

        // Re-fetch with details
        var result = new List<ShiftAssignmentDto>();
        foreach (var entity in entities)
        {
            var loaded = await _assignmentRepo.GetByIdWithDetailsAsync(entity.ShiftAssignmentId, ct);
            if (loaded is not null)
                result.Add(ShiftScheduleService.MapAssignment(loaded));
        }

        return result;
    }

    public async Task CancelAssignmentAsync(long assignmentId, CancellationToken ct = default)
    {
        var assignment = await _assignmentRepo.GetByIdAsync(assignmentId, ct)
                            ?? throw new NotFoundException("Shift assignment not found");

        var statusCode = assignment.AssignmentStatusLv?.ValueCode ?? "UNKNOWN";
        if (statusCode == nameof(ShiftAssignmentStatusCode.CANCELLED))
            throw new ValidationException("Assignment is already cancelled");

        // Cannot cancel if attendance has started (check-in exists)
        if (assignment.AttendanceRecord?.ActualCheckInAt is not null)
            throw new ConflictException("Cannot cancel assignment after staff has checked in");

        var cancelledStatusId = await ShiftAssignmentStatusCode.CANCELLED.ToShiftAssignmentStatusIdAsync(_lookupResolver, ct);
        assignment.AssignmentStatusLvId = cancelledStatusId;

        await _unitOfWork.SaveChangesAsync(ct);
    }
}
