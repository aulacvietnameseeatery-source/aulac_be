using System;
using System.Collections.Generic;

namespace Core.Entity;

public partial class ShiftAssignment
{
    public long ShiftAssignmentId { get; set; }

    /// <summary>FK ? shift_schedule.</summary>
    public long ShiftScheduleId { get; set; }

    /// <summary>FK ? staff_account.account_id.</summary>
    public long StaffId { get; set; }

    /// <summary>Snapshot of assigned role at time of scheduling.</summary>
    public long RoleId { get; set; }

    /// <summary>FK ? lookup_value (SHIFT_ASSIGNMENT_STATUS): ASSIGNED, CONFIRMED, CANCELLED.</summary>
    public uint AssignmentStatusLvId { get; set; }

    public long AssignedBy { get; set; }

    public DateTime AssignedAt { get; set; }

    public string? Remarks { get; set; }

    // Navigation 

    public virtual ShiftSchedule ShiftSchedule { get; set; } = null!;

    public virtual StaffAccount Staff { get; set; } = null!;

    public virtual Role Role { get; set; } = null!;

    public virtual LookupValue AssignmentStatusLv { get; set; } = null!;

    public virtual StaffAccount AssignedByStaff { get; set; } = null!;

    public virtual AttendanceRecord? AttendanceRecord { get; set; }
}
