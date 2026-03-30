namespace Core.DTO.Account;

/// <summary>
/// Shared pagination + date-range filter for account sub-resource endpoints.
/// </summary>
public class AccountSubResourceQueryDTO
{
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? Search { get; set; }
}
