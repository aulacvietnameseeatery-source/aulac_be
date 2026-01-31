using System;
using System.Collections.Generic;

namespace Core.Entity;

public partial class IngredientSupplier
{
    public long IngredientSupplierId { get; set; }

    public long? SupplierId { get; set; }

    public long? IngredientId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Ingredient? Ingredient { get; set; }

    public virtual Supplier? Supplier { get; set; }
}
