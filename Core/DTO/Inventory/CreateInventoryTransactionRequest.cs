using System.ComponentModel.DataAnnotations;

namespace Core.DTO.Inventory;

/// <summary>
/// Request to create a new inventory transaction (starts as DRAFT).
/// </summary>
public class CreateInventoryTransactionRequest
{
    [Required]
    public uint TypeLvId { get; set; }

    /// <summary>
    /// Required for OUT transactions. FK → LookupValue (EXPORT_REASON).
    /// </summary>
    public uint? ExportReasonLvId { get; set; }

    /// <summary>
    /// Required for IN transactions.
    /// </summary>
    public long? SupplierId { get; set; }

    /// <summary>
    /// Free-text area note, used for stock-check (ADJUST) transactions.
    /// </summary>
    [MaxLength(500)]
    public string? StockCheckAreaNote { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "At least one item is required")]
    public List<TransactionItemRequest> Items { get; set; } = new();
}
