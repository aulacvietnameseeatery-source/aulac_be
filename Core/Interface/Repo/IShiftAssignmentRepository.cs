using Core.DTO.Shift;
using Core.Entity;

namespace Core.Interface.Repo;

public interface IShiftAssignmentRepository
{
    void Add(ShiftAssignment entity);

    Task<ShiftAssignment?> GetByIdAsync(long id, CancellationToken ct = default);

    Task<ShiftAssignment?> GetByIdWithDetailsAsync(long id, CancellationToken ct = default);

    Task<(List<ShiftAssignment> Items, int TotalCount)> GetAssignmentsAsync(
        GetShiftAssignmentRequest request, CancellationToken ct = default);

    /// <summary>
    /// Returns true if the given staff already has an active (non-cancelled) assignment
    /// overlapping the given time window on the same date.
    /// </summary>
    Task<bool> HasOverlappingAssignmentAsync(
        long staffId, DateOnly workDate, DateTime plannedStart, DateTime plannedEnd,
        long? excludeAssignmentId = null, CancellationToken ct = default);

    /// <summary>
    /// Returns the staff IDs from the given list who already have an active assignment
    /// for the same template on the same work date.
    /// </summary>
    Task<List<long>> GetAlreadyAssignedStaffIdsAsync(
        long shiftTemplateId, DateOnly workDate, IEnumerable<long> staffIds,
        CancellationToken ct = default);

    // Report helpers
    Task<List<ShiftAssignment>> GetWorkedHoursDataAsync(
        DateOnly fromDate, DateOnly toDate, long? staffId, CancellationToken ct = default);

    Task<List<ShiftAssignment>> GetExceptionDataAsync(
        DateOnly fromDate, DateOnly toDate, long? staffId, CancellationToken ct = default);

    Task<(List<ShiftAssignment> Items, int TotalCount)> GetAttendanceReportAsync(
        AttendanceReportRequest request, CancellationToken ct = default);
}
