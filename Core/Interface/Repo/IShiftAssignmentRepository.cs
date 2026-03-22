using Core.DTO.Shift;
using Core.Entity;

namespace Core.Interface.Repo;

public interface IShiftAssignmentRepository
{
    void Add(ShiftAssignment entity);
    void AddRange(IEnumerable<ShiftAssignment> entities);

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

    /// <summary>Returns all DRAFT assignments in a date range (for publish workflow).</summary>
    Task<List<ShiftAssignment>> GetDraftAssignmentsAsync(
        DateOnly fromDate, DateOnly toDate, CancellationToken ct = default);

    /// <summary>Returns assignments by IDs with navigation includes.</summary>
    Task<List<ShiftAssignment>> GetByIdsWithDetailsAsync(
        IEnumerable<long> ids, CancellationToken ct = default);

    /// <summary>Returns all active assignments for a week starting on weekStart (7 days), used for copy-week.</summary>
    Task<List<ShiftAssignment>> GetWeekAssignmentsAsync(
        DateOnly weekStart, CancellationToken ct = default);

    /// <summary>Team schedule: returns all active assignments for a week, optionally filtered by template.</summary>
    Task<List<ShiftAssignment>> GetTeamScheduleAsync(
        DateOnly weekStart, DateOnly weekEnd, long? shiftTemplateId, CancellationToken ct = default);

    /// <summary>Calculates the total scheduled minutes for a staff member in a given week (Mon-Sun).</summary>
    Task<int> GetWeeklyScheduledMinutesAsync(
        long staffId, DateOnly weekStart, CancellationToken ct = default);

    /// <summary>Returns assignments that started before threshold and have no check-in (for no-show job).</summary>
    Task<List<ShiftAssignment>> GetNoShowCandidatesAsync(
        DateTime thresholdUtc, CancellationToken ct = default);
}
