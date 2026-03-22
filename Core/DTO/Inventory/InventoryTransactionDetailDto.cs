namespace Core.DTO.Inventory;

/// <summary>
/// Full transaction detail DTO (includes items + media).
/// </summary>
public class InventoryTransactionDetailDto
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
    public string? ExportReasonCode { get; set; }
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
    public string? StockCheckAreaNote { get; set; }

    public List<TransactionItemDto> Items { get; set; } = new();
    public List<TransactionMediaDto> Media { get; set; } = new();
}
