namespace Core.DTO.Shift;

// Attendance Report DTOs

public class AttendanceSummaryReportDto
{
    public DateOnly BusinessDate { get; set; }
    public string ShiftTypeCode { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;    
    public int Scheduled { get; set; }
    public int CheckedIn { get; set; }
    public int Active { get; set; }
    public int Completed { get; set; }
    public int Late { get; set; }
    public int Absent { get; set; }
    public int EarlyLeave { get; set; }
    public double AttendanceRate { get; set; }
}

public class AttendanceExceptionReportRowDto
{
    public long StaffId { get; set; }
    public string StaffName { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public DateOnly BusinessDate { get; set; }
    public string ShiftTypeCode { get; set; } = string.Empty;
    public string ExceptionType { get; set; } = string.Empty;
    public int MinutesAffected { get; set; }
    public bool IsManualAdjustment { get; set; }
    public string? ReviewerName { get; set; }
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
