using Core.DTO.Shift;

namespace Core.Interface.Service.Shift;

public interface IAttendanceService
{
    /// <summary>Staff check-in for an assigned shift.</summary>
    Task<AttendanceRecordDto> CheckInAsync(long assignmentId, CancellationToken ct = default);

    /// <summary>Staff check-out for an assigned shift.</summary>
    Task<AttendanceRecordDto> CheckOutAsync(long assignmentId, CancellationToken ct = default);

    /// <summary>Manager adjustment of an attendance record.</summary>
    Task<AttendanceRecordDto> AdjustAttendanceAsync(
        long attendanceId, AdjustAttendanceRequest request, long reviewerStaffId, CancellationToken ct = default);

    /// <summary>Returns live board data for the current business date.</summary>
    Task<LiveShiftBoardDto> GetLiveBoardAsync(DateOnly? businessDate = null, CancellationToken ct = default);

    /// <summary>Attendance report with paging.</summary>
    Task<(List<ShiftAssignmentDto> Items, int TotalCount)> GetAttendanceReportAsync(
        AttendanceReportRequest request, CancellationToken ct = default);

    /// <summary>Exceptions report (late, absent, early leave, manual adjustments).</summary>
    Task<List<AttendanceExceptionReportRowDto>> GetExceptionsReportAsync(
        DateOnly fromDate, DateOnly toDate, long? staffId, CancellationToken ct = default);

    /// <summary>Worked-hours report (scheduled vs actual).</summary>
    Task<List<WorkedHoursReportRowDto>> GetWorkedHoursReportAsync(
        DateOnly fromDate, DateOnly toDate, long? staffId, CancellationToken ct = default);
}
