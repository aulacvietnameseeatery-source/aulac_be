using System;
using System.Collections.Generic;

namespace Core.Entity;

public partial class InventoryTransactionItem
{
    public long TransactionItemId { get; set; }

    public long TransactionId { get; set; }

    public long IngredientId { get; set; }

    public decimal Quantity { get; set; }

    public string Unit { get; set; } = null!;

    public string? Note { get; set; }

    public virtual Ingredient Ingredient { get; set; } = null!;

    public virtual InventoryTransaction Transaction { get; set; } = null!;
}
