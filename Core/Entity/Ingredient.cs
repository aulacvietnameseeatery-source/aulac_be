using System;
using System.Collections.Generic;

namespace Core.Entity;

public partial class Ingredient
{
    public long IngredientId { get; set; }

    public string IngredientName { get; set; } = null!;

    public string Unit { get; set; } = null!;

    public uint? TypeLvId { get; set; }

    public long? IngredientNameTextId { get; set; }

    public long? ImageId { get; set; }

    public virtual CurrentStock? CurrentStock { get; set; }

    public virtual MediaAsset? Image { get; set; }

    public virtual I18nText? IngredientNameText { get; set; }

    public virtual ICollection<IngredientSupplier> IngredientSuppliers { get; set; } = new List<IngredientSupplier>();

    public virtual ICollection<InventoryTransactionItem> InventoryTransactionItems { get; set; } = new List<InventoryTransactionItem>();

    public virtual ICollection<Recipe> Recipes { get; set; } = new List<Recipe>();

    public virtual LookupValue? TypeLv { get; set; }
}
