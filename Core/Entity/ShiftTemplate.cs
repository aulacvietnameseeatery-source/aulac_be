using System;
using System.Collections.Generic;

namespace Core.Entity;

public partial class ShiftTemplate
{
    public long ShiftTemplateId { get; set; }

    /// <summary>Display name for this template, e.g. "Morning", "Lunch", "Dinner".</summary>
    public string TemplateName { get; set; } = null!;

    /// <summary>Default start time used to populate ShiftAssignment.PlannedStartAt when not overridden.</summary>
    public TimeOnly DefaultStartTime { get; set; }

    /// <summary>Default end time used to populate ShiftAssignment.PlannedEndAt when not overridden.</summary>
    public TimeOnly DefaultEndTime { get; set; }

    public string? Description { get; set; }

    /// <summary>Minutes before planned start that check-in is allowed. Overrides global AttendanceOptions when set.</summary>
    public int? BufferBeforeMinutes { get; set; }

    /// <summary>Minutes after planned end that check-out is still accepted. Overrides global AttendanceOptions when set.</summary>
    public int? BufferAfterMinutes { get; set; }

    public bool IsActive { get; set; }

    public long CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public long? UpdatedBy { get; set; }

    public DateTime UpdatedAt { get; set; }

    // Navigation

    public virtual StaffAccount CreatedByStaff { get; set; } = null!;

    public virtual StaffAccount? UpdatedByStaff { get; set; }

    public virtual ICollection<ShiftAssignment> ShiftAssignments { get; set; } = new List<ShiftAssignment>();
}
