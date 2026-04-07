namespace Core.DTO.Account;

/// <summary>
/// Audit-log row shown in the account detail → Audit Logs tab.
/// </summary>
public class AccountAuditLogDTO
{
    public long LogId { get; set; }
    public string? ActionCode { get; set; }
    public string? TargetTable { get; set; }
    public long? TargetId { get; set; }
    public DateTime? CreatedAt { get; set; }
}
