using System;

namespace Core.Entity;

public partial class AttendanceRecord
{
    public long AttendanceId { get; set; }

    /// <summary>FK ? shift_assignment (unique — one record per assignment).</summary>
    public long ShiftAssignmentId { get; set; }

    /// <summary>FK ? lookup_value (ATTENDANCE_STATUS): SCHEDULED, ACTIVE, COMPLETED, LATE, ABSENT, EARLY_LEAVE, EXCUSED.</summary>
    public uint AttendanceStatusLvId { get; set; }

    public DateTime? ActualCheckInAt { get; set; }

    public DateTime? ActualCheckOutAt { get; set; }

    /// <summary>Minutes late beyond grace period. 0 when on time.</summary>
    public int LateMinutes { get; set; }

    /// <summary>Minutes left before planned end minus buffer. 0 when normal.</summary>
    public int EarlyLeaveMinutes { get; set; }

    /// <summary>Actual worked minutes (check-out ? check-in).</summary>
    public int WorkedMinutes { get; set; }

    /// <summary>True when a manager has manually adjusted this record.</summary>
    public bool IsManualAdjustment { get; set; }

    /// <summary>Required when IsManualAdjustment is true.</summary>
    public string? AdjustmentReason { get; set; }

    /// <summary>FK ? staff_account (manager who reviewed/adjusted).</summary>
    public long? ReviewedBy { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    // ?? Navigation ??

    public virtual ShiftAssignment ShiftAssignment { get; set; } = null!;

    public virtual LookupValue AttendanceStatusLv { get; set; } = null!;

    public virtual StaffAccount? ReviewedByStaff { get; set; }
}
