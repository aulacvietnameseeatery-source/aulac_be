using System;
using System.Collections.Generic;

namespace Core.Entity;

public partial class InventoryTransaction
{
    public long TransactionId { get; set; }

    public long IngredientId { get; set; }

    public long? PurchaseId { get; set; }

    public long? SupplierId { get; set; }

    public decimal Quantity { get; set; }

    public long TypeId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Ingredient Ingredient { get; set; } = null!;

    public virtual Purchase? Purchase { get; set; }

    public virtual Supplier? Supplier { get; set; }

    public virtual SettingItem Type { get; set; } = null!;
}
