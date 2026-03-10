using System.ComponentModel.DataAnnotations;

namespace Core.DTO.Shift;

//  Schedule Requests

public class GetShiftScheduleRequest
{
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public uint? ShiftTypeLvId { get; set; }
    public uint? StatusLvId { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class CreateShiftScheduleRequest
{
    [Required]
    public DateOnly BusinessDate { get; set; }

    [Required]
    public uint ShiftTypeLvId { get; set; }

    [Required]
    public DateTime PlannedStartAt { get; set; }

    [Required]
    public DateTime PlannedEndAt { get; set; }

    public string? Notes { get; set; }
}

public class UpdateShiftScheduleRequest
{
    public DateTime? PlannedStartAt { get; set; }
    public DateTime? PlannedEndAt { get; set; }
    public uint? StatusLvId { get; set; }
    public string? Notes { get; set; }
}

// Assignment Requests

public class GetShiftAssignmentRequest
{
    public long? ShiftScheduleId { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public long? StaffId { get; set; }
    public uint? AssignmentStatusLvId { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class CreateShiftAssignmentRequest
{
    [Required]
    public long ShiftScheduleId { get; set; }

    [Required]
    [MinLength(1)]
    public List<long> StaffIds { get; set; } = [];

    public string? Remarks { get; set; }
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
    public uint? ShiftTypeLvId { get; set; }
    public uint? AttendanceStatusLvId { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
