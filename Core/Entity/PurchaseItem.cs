using System;
using System.Collections.Generic;

namespace Core.Entity;

public partial class PurchaseItem
{
    public long PurchaseItemId { get; set; }

    public long PurchaseId { get; set; }

    public long IngredientId { get; set; }

    public decimal Quantity { get; set; }

    public decimal? UnitPrice { get; set; }

    public virtual Ingredient Ingredient { get; set; } = null!;

    public virtual Purchase Purchase { get; set; } = null!;
}
