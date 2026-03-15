namespace Core.DTO.Shift;

// Assignment DTOs

public class ShiftAssignmentListDto
{
    public long ShiftAssignmentId { get; set; }
    public long ShiftTemplateId { get; set; }
    public string TemplateName { get; set; } = string.Empty;
    public long StaffId { get; set; }
    public string StaffName { get; set; } = string.Empty;
    public DateOnly WorkDate { get; set; }
    public DateTime PlannedStartAt { get; set; }
    public DateTime PlannedEndAt { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
    public DateTime AssignedAt { get; set; }
    public string AssignedByName { get; set; } = string.Empty;
}

public class ShiftAssignmentDetailDto : ShiftAssignmentListDto
{
    public AttendanceRecordDto? Attendance { get; set; }
}

// Attendance DTOs

public class AttendanceRecordDto
{
    public long AttendanceId { get; set; }
    public long ShiftAssignmentId { get; set; }
    public string AttendanceStatusCode { get; set; } = string.Empty;
    public string AttendanceStatusName { get; set; } = string.Empty;
    public DateTime? ActualCheckInAt { get; set; }
    public DateTime? ActualCheckOutAt { get; set; }
    public int LateMinutes { get; set; }
    public int EarlyLeaveMinutes { get; set; }
    public int WorkedMinutes { get; set; }
    public bool IsManualAdjustment { get; set; }
    public string? AdjustmentReason { get; set; }
    public string? ReviewedByName { get; set; }
    public DateTime? ReviewedAt { get; set; }
}

// Report DTOs

public class AttendanceReportRowDto
{
    public long ShiftAssignmentId { get; set; }
    public long StaffId { get; set; }
    public string StaffName { get; set; } = string.Empty;
    public DateOnly WorkDate { get; set; }
    public string TemplateName { get; set; } = string.Empty;
    public DateTime PlannedStartAt { get; set; }
    public DateTime PlannedEndAt { get; set; }
    public string AttendanceStatusCode { get; set; } = string.Empty;
    public DateTime? ActualCheckInAt { get; set; }
    public DateTime? ActualCheckOutAt { get; set; }
    public int LateMinutes { get; set; }
    public int EarlyLeaveMinutes { get; set; }
    public int WorkedMinutes { get; set; }
    public bool IsManualAdjustment { get; set; }
}

public class WorkedHoursReportRowDto
{
    public long StaffId { get; set; }
    public string StaffName { get; set; } = string.Empty;
    public int ScheduledMinutes { get; set; }
    public int WorkedMinutes { get; set; }
    public int VarianceMinutes { get; set; }
    public int IncompleteRecords { get; set; }
}

public class AttendanceExceptionReportRowDto
{
    public long StaffId { get; set; }
    public string StaffName { get; set; } = string.Empty;
    public DateOnly WorkDate { get; set; }
    public string TemplateName { get; set; } = string.Empty;
    public string ExceptionType { get; set; } = string.Empty;
    public int MinutesAffected { get; set; }
    public bool IsManualAdjustment { get; set; }
    public string? ReviewerName { get; set; }
}
