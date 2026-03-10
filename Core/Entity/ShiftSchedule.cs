using System;
using System.Collections.Generic;

namespace Core.Entity;

public partial class ShiftSchedule
{
    public long ShiftScheduleId { get; set; }

    /// <summary>Restaurant operating date for this shift.</summary>
    public DateOnly BusinessDate { get; set; }

    /// <summary>FK ? lookup_value (SHIFT_TYPE): MORNING, LUNCH, EVENING.</summary>
    public uint ShiftTypeLvId { get; set; }

    /// <summary>FK ? lookup_value (SHIFT_STATUS): DRAFT, PUBLISHED, CLOSED, CANCELLED.</summary>
    public uint StatusLvId { get; set; }

    public DateTime PlannedStartAt { get; set; }

    public DateTime PlannedEndAt { get; set; }

    public string? Notes { get; set; }

    public long CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public long? UpdatedBy { get; set; }

    public DateTime UpdatedAt { get; set; }

    // ?? Navigation ??

    public virtual LookupValue ShiftTypeLv { get; set; } = null!;

    public virtual LookupValue StatusLv { get; set; } = null!;

    public virtual StaffAccount CreatedByStaff { get; set; } = null!;

    public virtual StaffAccount? UpdatedByStaff { get; set; }

    public virtual ICollection<ShiftAssignment> ShiftAssignments { get; set; } = new List<ShiftAssignment>();
}
