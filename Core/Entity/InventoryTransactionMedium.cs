using System;
using System.Collections.Generic;

namespace Core.Entity;

public partial class InventoryTransactionMedium
{
    public long TransactionId { get; set; }

    public long MediaId { get; set; }

    public bool? IsPrimary { get; set; }

    public virtual MediaAsset Media { get; set; } = null!;

    public virtual InventoryTransaction Transaction { get; set; } = null!;
}
