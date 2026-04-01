namespace Core.DTO.Account;

/// <summary>
/// Lightweight order summary shown in the account detail → Orders tab.
/// </summary>
public class AccountOrderSummaryDTO
{
    public long OrderId { get; set; }
    public string? TableCode { get; set; }
    public string? CustomerName { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal? TipAmount { get; set; }
    public string OrderStatus { get; set; } = "";
    public string Source { get; set; } = "";
    public DateTime? CreatedAt { get; set; }
    public int ItemCount { get; set; }
    public bool IsPaid { get; set; }
}
