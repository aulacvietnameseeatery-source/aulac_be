namespace Core.DTO.Inventory;

/// <summary>
/// Filter/query parameters for listing inventory transactions.
/// </summary>
public class GetTransactionsFilterRequest
{
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Searches by TransactionCode or Note.
    /// </summary>
    public string? Search { get; set; }

    /// <summary>
    /// Filter by transaction type LookupValue ID (IN, OUT, ADJUST).
    /// </summary>
    public uint? TypeLvId { get; set; }

    /// <summary>
    /// Filter by transaction status LookupValue ID.
    /// </summary>
    public uint? StatusLvId { get; set; }

    /// <summary>
    /// Filter by export reason LookupValue ID.
    /// </summary>
    public uint? ExportReasonLvId { get; set; }

    /// <summary>
    /// Filter by supplier.
    /// </summary>
    public long? SupplierId { get; set; }

    /// <summary>
    /// Filter transactions created from this date (inclusive).
    /// </summary>
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// Filter transactions created to this date (inclusive).
    /// </summary>
    public DateTime? ToDate { get; set; }
}
