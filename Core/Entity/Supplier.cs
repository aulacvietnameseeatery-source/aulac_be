using System;
using System.Collections.Generic;

namespace Core.Entity;

public partial class Supplier
{
    public long SupplierId { get; set; }

    public string SupplierName { get; set; } = null!;

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public virtual ICollection<IngredientSupplier> IngredientSuppliers { get; set; } = new List<IngredientSupplier>();

    public virtual ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();
}
