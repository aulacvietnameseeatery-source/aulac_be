namespace Core.DTO.Account;

/// <summary>
/// Service-error row shown in the account detail → Service Errors tab.
/// </summary>
public class AccountServiceErrorDTO
{
    public long ErrorId { get; set; }
    public long? OrderId { get; set; }
    public string CategoryName { get; set; } = "";
    public string CategoryCode { get; set; } = "";
    public string Description { get; set; } = "";
    public string SeverityName { get; set; } = "";
    public decimal? PenaltyAmount { get; set; }
    public bool IsResolved { get; set; }
    public string? ResolvedByName { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? CreatedAt { get; set; }
}
