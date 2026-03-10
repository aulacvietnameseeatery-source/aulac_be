namespace Core.DTO.Shift;

// ?? Schedule DTOs ??????????????????????????????????????????????????

public class ShiftScheduleListDto
{
    public long ShiftScheduleId { get; set; }
    public DateOnly BusinessDate { get; set; }
    public uint ShiftTypeLvId { get; set; }
    public string ShiftTypeCode { get; set; } = string.Empty;
    public string ShiftTypeName { get; set; } = string.Empty;
    public uint StatusLvId { get; set; }
    public string StatusCode { get; set; } = string.Empty;
    public string StatusName { get; set; } = string.Empty;
    public DateTime PlannedStartAt { get; set; }
    public DateTime PlannedEndAt { get; set; }
    public string? Notes { get; set; }
    public int AssignmentCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ShiftScheduleDetailDto : ShiftScheduleListDto
{
    public string CreatedByName { get; set; } = string.Empty;
    public string? UpdatedByName { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<ShiftAssignmentDto> Assignments { get; set; } = [];
}

// Assignment DTOs

public class ShiftAssignmentDto
{
    public long ShiftAssignmentId { get; set; }
    public long ShiftScheduleId { get; set; }
    public long StaffId { get; set; }
    public string StaffName { get; set; } = string.Empty;
    public long RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public uint AssignmentStatusLvId { get; set; }
    public string AssignmentStatusCode { get; set; } = string.Empty;
    public string AssignmentStatusName { get; set; } = string.Empty;
    public string? Remarks { get; set; }
    public DateTime AssignedAt { get; set; }
    public string AssignedByName { get; set; } = string.Empty;
    public AttendanceRecordDto? Attendance { get; set; }
}

// ?? Attendance DTOs ????????????????????????????????????????????????

public class AttendanceRecordDto
{
    public long AttendanceId { get; set; }
    public long ShiftAssignmentId { get; set; }
    public uint AttendanceStatusLvId { get; set; }
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

// ?? Live Board DTOs ????????????????????????????????????????????????

public class LiveShiftBoardDto
{
    public DateOnly BusinessDate { get; set; }
  public LiveBoardSummary Summary { get; set; } = new();
    public List<LiveShiftBoardRowDto> Rows { get; set; } = [];
}

public class LiveBoardSummary
{
    public int Scheduled { get; set; }
public int Active { get; set; }
    public int Late { get; set; }
    public int Absent { get; set; }
    public int Completed { get; set; }
}

public class LiveShiftBoardRowDto
{
    public long ShiftAssignmentId { get; set; }
    public long StaffId { get; set; }
    public string StaffName { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string ShiftTypeCode { get; set; } = string.Empty;
    public DateTime PlannedStartAt { get; set; }
    public DateTime PlannedEndAt { get; set; }
    public DateTime? ActualCheckInAt { get; set; }
    public DateTime? ActualCheckOutAt { get; set; }
    public string AttendanceStatusCode { get; set; } = string.Empty;
    public int LateMinutes { get; set; }
}
