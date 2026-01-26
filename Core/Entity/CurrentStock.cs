using System;
using System.Collections.Generic;

namespace Core.Entity;

public partial class CurrentStock
{
    public long IngredientId { get; set; }

    public decimal QuantityOnHand { get; set; }

    public DateTime? LastUpdatedAt { get; set; }

    public virtual Ingredient Ingredient { get; set; } = null!;
}
