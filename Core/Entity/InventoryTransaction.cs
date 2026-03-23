using System;
using System.Collections.Generic;

namespace Core.Entity;

public partial class InventoryTransaction
{
    public long TransactionId { get; set; }

    /// <summary>
    /// Auto-generated code: {TYPE}-{YYYYMMDD}-{SEQ} (e.g. IN-20250101-001)
    /// </summary>
    public string? TransactionCode { get; set; }

    public long? CreatedBy { get; set; }

    public DateTime? CreatedAt { get; set; }

    /// <summary>
    /// When the creator submits the transaction for approval (moves DRAFT → PENDING_APPROVAL)
    /// </summary>
    public DateTime? SubmittedAt { get; set; }

    /// <summary>
    /// Staff who approved/rejected the transaction
    /// </summary>
    public long? ApprovedBy { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public string? Note { get; set; }

    public uint TypeLvId { get; set; }

    public uint StatusLvId { get; set; }

    public long? SupplierId { get; set; }

    /// <summary>
    /// FK → LookupValue (EXPORT_REASON): COOKING, SPOILED, EXPIRED, BROKEN, LOST, DISPOSED, WORN_OUT.
    /// Only applicable when TypeLvId = OUT.
    /// </summary>
    public uint? ExportReasonLvId { get; set; }

    /// <summary>
    /// Free-text area/section note for stock-check transactions (ADJUST type)
    /// </summary>
    public string? StockCheckAreaNote { get; set; }

    public virtual StaffAccount? CreatedByNavigation { get; set; }

    public virtual StaffAccount? ApprovedByNavigation { get; set; }

    public virtual ICollection<InventoryTransactionItem> InventoryTransactionItems { get; set; } = new List<InventoryTransactionItem>();

    public virtual ICollection<InventoryTransactionMedium> InventoryTransactionMedia { get; set; } = new List<InventoryTransactionMedium>();

    public virtual LookupValue StatusLv { get; set; } = null!;

    public virtual Supplier? Supplier { get; set; }

    public virtual LookupValue TypeLv { get; set; } = null!;

    public virtual LookupValue? ExportReasonLv { get; set; }
}
