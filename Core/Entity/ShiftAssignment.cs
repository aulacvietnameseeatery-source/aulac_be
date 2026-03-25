using System;
using System.Collections.Generic;

namespace Core.Entity;

public partial class ShiftAssignment
{
    public long ShiftAssignmentId { get; set; }

    /// <summary>FK → shift_template.</summary>
    public long ShiftTemplateId { get; set; }

    /// <summary>FK → staff_account.account_id.</summary>
    public long StaffId { get; set; }

    /// <summary>The date this shift occurs on.</summary>
    public DateOnly WorkDate { get; set; }

    /// <summary>Planned shift start (copied from template defaults, can be overridden).</summary>
    public DateTime PlannedStartAt { get; set; }

    /// <summary>Planned shift end (copied from template defaults, can be overridden).</summary>
    public DateTime PlannedEndAt { get; set; }

    /// <summary>FK → lookup_value (SHIFT_ASSIGNMENT_STATUS): DRAFT, ASSIGNED, CONFIRMED, CANCELLED.</summary>
    public uint AssignmentStatusLvId { get; set; }

    /// <summary>False when assignment is cancelled (soft delete).</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Metadata tags, e.g. "SPLIT_SHIFT" for Ca Gãy.</summary>
    public string? Tags { get; set; }

    public string? Notes { get; set; }

    public long AssignedBy { get; set; }

    public DateTime AssignedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    // Navigation

    public virtual ShiftTemplate ShiftTemplate { get; set; } = null!;

    public virtual StaffAccount Staff { get; set; } = null!;

    public virtual StaffAccount AssignedByStaff { get; set; } = null!;

    public virtual LookupValue AssignmentStatusLv { get; set; } = null!;

    public virtual AttendanceRecord? AttendanceRecord { get; set; }
}
