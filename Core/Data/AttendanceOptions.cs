namespace Core.Data;

/// <summary>
/// Configuration thresholds for attendance status evaluation.
/// Bound from appsettings.json section "Attendance".
/// </summary>
public class AttendanceOptions
{
    /// <summary>How many minutes before planned start a staff member may check in.</summary>
    public int AllowedEarlyCheckInMinutes { get; set; } = 30;

    /// <summary>Grace period after planned start before marking as LATE.</summary>
    public int LateGraceMinutes { get; set; } = 5;

    /// <summary>Minutes after planned start with no check-in ? flag ABSENT.</summary>
    public int AbsenceThresholdMinutes { get; set; } = 30;

    /// <summary>Buffer before planned end — checking out earlier marks EARLY_LEAVE.</summary>
    public int EarlyLeaveBufferMinutes { get; set; } = 5;
}
