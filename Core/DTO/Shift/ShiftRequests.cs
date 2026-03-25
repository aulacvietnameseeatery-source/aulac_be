using System.ComponentModel.DataAnnotations;

namespace Core.DTO.Shift;

// Assignment Requests

public class GetShiftAssignmentRequest
{
    public long? StaffId { get; set; }
    public long? ShiftTemplateId { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public bool? IsActive { get; set; }
    public string? AssignmentStatusCode { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class CreateShiftAssignmentRequest
{
    [Required]
    public long ShiftTemplateId { get; set; }

    [Required]
    public long StaffId { get; set; }

    [Required]
    public DateOnly WorkDate { get; set; }

    /// <summary>Optional. Auto-populated from template's DefaultStartTime when omitted.</summary>
    public DateTime? PlannedStartAt { get; set; }

    /// <summary>Optional. Auto-populated from template's DefaultEndTime when omitted.</summary>
    public DateTime? PlannedEndAt { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    [MaxLength(200)]
    public string? Tags { get; set; }

    /// <summary>When true, the assignment is created in DRAFT status (not yet visible to staff).</summary>
    public bool IsDraft { get; set; }
}

public class BulkCreateAssignmentRequest
{
    [Required]
    public long ShiftTemplateId { get; set; }

    [Required]
    [MinLength(1)]
    public List<long> StaffIds { get; set; } = new();

    [Required]
    public DateOnly WorkDate { get; set; }

    public DateTime? PlannedStartAt { get; set; }
    public DateTime? PlannedEndAt { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    [MaxLength(200)]
    public string? Tags { get; set; }

    public bool IsDraft { get; set; }
}

public class PublishAssignmentsRequest
{
    /// <summary>Specific assignment IDs to publish. If empty, publishes all DRAFT in the date range.</summary>
    public List<long>? AssignmentIds { get; set; }

    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
}

public class CopyWeekRequest
{
    [Required]
    public DateOnly SourceWeekStart { get; set; }

    [Required]
    public DateOnly TargetWeekStart { get; set; }

    /// <summary>When true, creates assignments in DRAFT status.</summary>
    public bool AsDraft { get; set; } = true;
}

public class ReassignRequest
{
    [Required]
    public long NewStaffId { get; set; }

    /// <summary>Optional target work date (yyyy-MM-dd). If omitted, keeps current date.</summary>
    public DateOnly? NewWorkDate { get; set; }

    [MaxLength(500)]
    public string? Reason { get; set; }
}

public class ConfirmAssignmentRequest
{
    // placeholder for future: optional device ID or GPS
}

public class UpdateShiftAssignmentRequest
{
    public DateTime? PlannedStartAt { get; set; }
    public DateTime? PlannedEndAt { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    [MaxLength(200)]
    public string? Tags { get; set; }
}

// Attendance Requests

public class AdjustAttendanceRequest
{
    public DateTime? ActualCheckInAt { get; set; }
    public DateTime? ActualCheckOutAt { get; set; }

    [Required]
    [MaxLength(500)]
    public string AdjustmentReason { get; set; } = null!;
}

// Report Requests

public class AttendanceReportRequest
{
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public long? StaffId { get; set; }
    public long? ShiftTemplateId { get; set; }
    public string? AttendanceStatusCode { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>Request for the team (everyone) weekly schedule view.</summary>
public class TeamScheduleRequest
{
    [Required]
    public DateOnly WeekStart { get; set; }

    /// <summary>
    /// Optional range end date. If omitted, defaults to WeekStart + 6 days.
    /// </summary>
    public DateOnly? WeekEnd { get; set; }

    public long? ShiftTemplateId { get; set; }
}
