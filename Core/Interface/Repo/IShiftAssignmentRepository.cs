using Core.DTO.Shift;
using Core.Entity;

namespace Core.Interface.Repo;

public interface IShiftAssignmentRepository
{
    void Add(ShiftAssignment entity);
    void AddRange(IEnumerable<ShiftAssignment> entities);

    Task<ShiftAssignment?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<ShiftAssignment?> GetByIdWithDetailsAsync(long id, CancellationToken ct = default);

    Task<(List<ShiftAssignment> Items, int TotalCount)> GetAssignmentsAsync(GetShiftAssignmentRequest request, CancellationToken ct = default);

    /// <summary>
    /// Checks if a staff member already has an active (non-cancelled) assignment
    /// overlapping the given time window.
    /// </summary>
    Task<bool> HasOverlappingAssignmentAsync(
        long staffId, DateTime plannedStart, DateTime plannedEnd,
        long? excludeAssignmentId = null, CancellationToken ct = default);

    /// <summary>
    /// Checks if any listed staff are already assigned to the given schedule.
    /// Returns the IDs of those already assigned.
    /// </summary>
    Task<List<long>> GetAlreadyAssignedStaffIdsAsync(long shiftScheduleId, IEnumerable<long> staffIds, CancellationToken ct = default);
}
