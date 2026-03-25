namespace Core.DTO.Inventory;

/// <summary>
/// Summary DTO for transaction list views.
/// </summary>
public class InventoryTransactionListDto
{
    public long TransactionId { get; set; }
    public string? TransactionCode { get; set; }

    public uint TypeLvId { get; set; }
    public string? TypeCode { get; set; }
    public string? TypeName { get; set; }

    public uint StatusLvId { get; set; }
    public string? StatusCode { get; set; }
    public string? StatusName { get; set; }

    public uint? ExportReasonLvId { get; set; }
    public string? ExportReasonName { get; set; }

    public long? SupplierId { get; set; }
    public string? SupplierName { get; set; }

    public long? CreatedBy { get; set; }
    public string? CreatedByName { get; set; }

    public DateTime? CreatedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }

    public long? ApprovedBy { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTime? ApprovedAt { get; set; }

    public string? Note { get; set; }

    public int ItemCount { get; set; }

    /// <summary>
    /// Total value (sum of Quantity * UnitPrice) for IN transactions.
    /// </summary>
    public decimal? TotalValue { get; set; }
}
