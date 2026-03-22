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
    public string AssignmentStatusCode { get; set; } = string.Empty;
    public string AssignmentStatusName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string? Tags { get; set; }
    public string? Notes { get; set; }
    public DateTime AssignedAt { get; set; }
    public string AssignedByName { get; set; } = string.Empty;
}

public class ShiftAssignmentDetailDto : ShiftAssignmentListDto
{
    public AttendanceRecordDto? Attendance { get; set; }
}

public class ShiftLiveBoardItemDto
{
    public long ShiftAssignmentId { get; set; }
    public long ShiftTemplateId { get; set; }
    public string TemplateName { get; set; } = string.Empty;
    public long StaffId { get; set; }
    public string StaffName { get; set; } = string.Empty;
    public string StaffRoleCode { get; set; } = string.Empty;
    public string StaffRoleName { get; set; } = string.Empty;
    public DateOnly WorkDate { get; set; }
    public DateTime PlannedStartAt { get; set; }
    public DateTime PlannedEndAt { get; set; }
    public string AssignmentStatusCode { get; set; } = string.Empty;
    public string AssignmentStatusName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string? Tags { get; set; }
    public string? Notes { get; set; }
    public DateTime AssignedAt { get; set; }
    public string AssignedByName { get; set; } = string.Empty;
    public string? AttendanceStatusCode { get; set; }
    public string? AttendanceStatusName { get; set; }
    public DateTime? ActualCheckInAt { get; set; }
    public DateTime? ActualCheckOutAt { get; set; }
    public int LateMinutes { get; set; }
    public int EarlyLeaveMinutes { get; set; }
    public int WorkedMinutes { get; set; }
    public bool IsManualAdjustment { get; set; }
    public string LiveStatusCode { get; set; } = string.Empty;
    public string LiveStatusName { get; set; } = string.Empty;
    public bool HasAlert { get; set; }
    public string? CurrentTaskLabel { get; set; }
    public string? CurrentLocationLabel { get; set; }
    public int? OrdersHandledCount { get; set; }
    public int? PaidBillsCount { get; set; }
    public decimal? CurrentRevenue { get; set; }
    public int? ItemsCompletedCount { get; set; }
    public int? PendingTicketsCount { get; set; }
    public int IssueCount { get; set; }
    public string? LatestIssueText { get; set; }
}

public class ShiftLiveOrderSnapshotDto
{
    public long OrderId { get; set; }
    public long StaffId { get; set; }
    public string? TableCode { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LatestPaidAt { get; set; }
    public DateTime? LastActivityAt { get; set; }
    public decimal PaidRevenue { get; set; }
    public int CompletedItemsCount { get; set; }
    public int PendingItemsCount { get; set; }
}

public class ShiftLiveIssueSnapshotDto
{
    public long StaffId { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsResolved { get; set; }
    public DateTime? CreatedAt { get; set; }
    public string? TableCode { get; set; }
}

public class ShiftLiveRealtimeEventDto
{
    public string EventType { get; set; } = string.Empty;
    public DateOnly? WorkDate { get; set; }
    public long? ShiftAssignmentId { get; set; }
    public long? StaffId { get; set; }
    public long? OrderId { get; set; }
    public DateTime OccurredAt { get; set; }
}

// Attendance DTOs

public class TimeLogDto
{
    public long TimeLogId { get; set; }
    public DateTime PunchInTime { get; set; }
    public DateTime? PunchOutTime { get; set; }
    public string ValidationStatus { get; set; } = "Valid";
    public int PunchDurationMinutes { get; set; }
    public DateTime CreatedAt { get; set; }
}

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
    public List<TimeLogDto> TimeLogs { get; set; } = new();
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

// Snapshot DTO

public class ShiftReportSnapshotDto
{
    public int AttendanceCount { get; set; }
    public int WorkedStaffCount { get; set; }
    public int ExceptionCount { get; set; }
}
