using System;

namespace Core.Entity;

/// <summary>
/// Records a single punch-in / punch-out cycle within an attendance record.
/// Multiple TimeLogs per AttendanceRecord enable split-shift (Ca Gãy) tracking.
/// </summary>
public partial class TimeLog
{
    public long TimeLogId { get; set; }

    /// <summary>FK → attendance_record.</summary>
    public long AttendanceRecordId { get; set; }

    public DateTime PunchInTime { get; set; }

    public DateTime? PunchOutTime { get; set; }

    /// <summary>Placeholder for geofencing – "lat,lng" format.</summary>
    public string? GpsLocationIn { get; set; }

    /// <summary>Placeholder for geofencing – "lat,lng" format.</summary>
    public string? GpsLocationOut { get; set; }

    /// <summary>Placeholder for device binding – device identifier at punch-in.</summary>
    public string? DeviceIdIn { get; set; }

    /// <summary>Placeholder for device binding – device identifier at punch-out.</summary>
    public string? DeviceIdOut { get; set; }

    /// <summary>Valid, Late, Early_Leave, Missing_Punch.</summary>
    public string ValidationStatus { get; set; } = "Valid";

    /// <summary>Computed: PunchOutTime − PunchInTime in minutes. 0 when PunchOut is null.</summary>
    public int PunchDurationMinutes { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation

    public virtual AttendanceRecord AttendanceRecord { get; set; } = null!;
}
