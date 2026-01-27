using System;
using System.Collections.Generic;
using Core.Enum;

namespace Core.Entity;

public partial class InventoryTransaction
{
    public long TransactionId { get; set; }

    public InventoryTransactionType Type { get; set; }

    public PurchaseStatus Status { get; set; }

    public long? CreatedBy { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string? Note { get; set; }

    public virtual StaffAccount? CreatedByNavigation { get; set; }

    public virtual ICollection<InventoryTransactionItem> InventoryTransactionItems { get; set; } = new List<InventoryTransactionItem>();

    public virtual ICollection<InventoryTransactionMedium> InventoryTransactionMedia { get; set; } = new List<InventoryTransactionMedium>();
}
