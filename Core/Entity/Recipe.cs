using System;
using System.Collections.Generic;

namespace Core.Entity;

public partial class Recipe
{
    public long DishId { get; set; }

    public long IngredientId { get; set; }

    public decimal Quantity { get; set; }

    public string Unit { get; set; } = null!;

    public string? Note { get; set; }

    public virtual Dish Dish { get; set; } = null!;

    public virtual Ingredient Ingredient { get; set; } = null!;
}
