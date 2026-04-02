namespace Core.Data;

/// <summary>
/// Configuration thresholds for attendance status evaluation.
/// Bound from appsettings.json section "Attendance".
/// </summary>
public class AttendanceOptions
{
    /// <summary>How many minutes before planned start a staff member may check in (default: 2 hours).</summary>
    public int AllowedEarlyCheckInMinutes { get; set; } = 120;

    /// <summary>Grace period after planned start before marking as LATE.</summary>
    public int LateGraceMinutes { get; set; } = 5;

    /// <summary>Minutes after planned start with no check-in ? flag ABSENT.</summary>
    public int AbsenceThresholdMinutes { get; set; } = 30;

    /// <summary>Buffer before planned end � checking out earlier marks EARLY_LEAVE.</summary>
    public int EarlyLeaveBufferMinutes { get; set; } = 5;
    // ── Geofencing placeholders ──

    /// <summary>Base restaurant latitude for geofencing. Null = geofencing disabled.</summary>
    public double? BaseLatitude { get; set; }

    /// <summary>Base restaurant longitude for geofencing. Null = geofencing disabled.</summary>
    public double? BaseLongitude { get; set; }

    /// <summary>Maximum allowed distance from base location in metres.</summary>
    public int MaxRadiusMeters { get; set; } = 200;

    // ── No-show & weekly limits ──

    /// <summary>Minutes after planned start with no check-in before sending a no-show alert.</summary>
    public int NoShowThresholdMinutes { get; set; } = 15;

    /// <summary>Soft limit for total scheduled hours per week (used for conflict warnings).</summary>
    public double MaxWeeklyHours { get; set; } = 48;}
