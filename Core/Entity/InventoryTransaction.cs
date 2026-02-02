using System;
using System.Collections.Generic;

namespace Core.Entity;

public partial class InventoryTransaction
{
    public long TransactionId { get; set; }

    public long? CreatedBy { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string? Note { get; set; }

    public uint TypeLvId { get; set; }

    public uint StatusLvId { get; set; }

    public long? SupplierId { get; set; }

    public virtual StaffAccount? CreatedByNavigation { get; set; }

    public virtual ICollection<InventoryTransactionItem> InventoryTransactionItems { get; set; } = new List<InventoryTransactionItem>();

    public virtual ICollection<InventoryTransactionMedium> InventoryTransactionMedia { get; set; } = new List<InventoryTransactionMedium>();

    public virtual LookupValue StatusLv { get; set; } = null!;

    public virtual Supplier? Supplier { get; set; }

    public virtual LookupValue TypeLv { get; set; } = null!;
}
