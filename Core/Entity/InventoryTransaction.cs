using System;
using System.Collections.Generic;

namespace Core.Entity;

public partial class InventoryTransaction
{
    public long TransactionId { get; set; }

    public long DirectionId { get; set; }

    public long ReasonId { get; set; }

    public long? CreatedBy { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string? Note { get; set; }

    public virtual StaffAccount? CreatedByNavigation { get; set; }

    public virtual SettingItem Direction { get; set; } = null!;

    public virtual ICollection<InventoryTransactionItem> InventoryTransactionItems { get; set; } = new List<InventoryTransactionItem>();

    public virtual ICollection<InventoryTransactionMedium> InventoryTransactionMedia { get; set; } = new List<InventoryTransactionMedium>();

    public virtual SettingItem Reason { get; set; } = null!;
}
