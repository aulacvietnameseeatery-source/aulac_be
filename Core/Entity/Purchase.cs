using System;
using System.Collections.Generic;

namespace Core.Entity;

public partial class Purchase
{
    public long PurchaseId { get; set; }

    public long SupplierId { get; set; }

    public long StaffId { get; set; }

    public decimal? TotalAmount { get; set; }

    public DateTime? PurchasedAt { get; set; }

    public virtual ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();

    public virtual ICollection<PurchaseItem> PurchaseItems { get; set; } = new List<PurchaseItem>();

    public virtual Account Staff { get; set; } = null!;

    public virtual Supplier Supplier { get; set; } = null!;
}
