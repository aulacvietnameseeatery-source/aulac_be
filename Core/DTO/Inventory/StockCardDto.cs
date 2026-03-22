namespace Core.DTO.Inventory;

/// <summary>
/// Stock card: paginated movement history for a single ingredient.
/// </summary>
public class StockCardDto
{
    public long TransactionItemId { get; set; }
    public long TransactionId { get; set; }
    public string? TransactionCode { get; set; }

    public string? TypeCode { get; set; }
    public string? TypeName { get; set; }

    public string? StatusCode { get; set; }

    public decimal QuantityChanged { get; set; }

    public string? UnitName { get; set; }

    public decimal? UnitPrice { get; set; }

    public string? ExportReasonName { get; set; }

    public string? Note { get; set; }

    public string? CreatedByName { get; set; }
    public DateTime? CreatedAt { get; set; }
}
