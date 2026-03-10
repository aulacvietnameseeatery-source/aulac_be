using Core.DTO.Shift;
using Core.Entity;

namespace Core.Interface.Repo;

public interface IAttendanceRepository
{
    void Add(AttendanceRecord entity);

    Task<AttendanceRecord?> GetByAssignmentIdAsync(long shiftAssignmentId, CancellationToken ct = default);

    Task<AttendanceRecord?> GetByIdAsync(long attendanceId, CancellationToken ct = default);

    Task<AttendanceRecord?> GetByIdWithDetailsAsync(long attendanceId, CancellationToken ct = default);

    /// <summary>
    /// Returns all assignments + attendance for schedules on the given business date.
    /// Used by the live board.
    /// </summary>
    Task<List<ShiftAssignment>> GetLiveBoardDataAsync(DateOnly businessDate, CancellationToken ct = default);

    /// <summary>
    /// Returns assignments with attendance for reporting within a date range.
    /// </summary>
    Task<(List<ShiftAssignment> Items, int TotalCount)> GetAttendanceReportAsync(AttendanceReportRequest request, CancellationToken ct = default);

    /// <summary>
    /// Returns worked-hours aggregation data for the given date range.
    /// </summary>
    Task<List<ShiftAssignment>> GetWorkedHoursDataAsync(DateOnly fromDate, DateOnly toDate, long? staffId, CancellationToken ct = default);

    /// <summary>
    /// Returns attendance exception rows for the given date range.
    /// </summary>
    Task<List<ShiftAssignment>> GetExceptionDataAsync(
        DateOnly fromDate, DateOnly toDate, long? staffId, CancellationToken ct = default);
}
