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
}

public class UpdateShiftAssignmentRequest
{
    public DateTime? PlannedStartAt { get; set; }
    public DateTime? PlannedEndAt { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
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
