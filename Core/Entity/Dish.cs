using System;
using System.Collections.Generic;

namespace Core.Entity;

public partial class Dish
{
    public long DishId { get; set; }

    public long CategoryId { get; set; }

    public string DishName { get; set; } = null!;

    public decimal Price { get; set; }

    public DateTime? CreatedAt { get; set; }

    public uint DishStatusLvId { get; set; }

    public virtual DishCategory Category { get; set; } = null!;

    public virtual ICollection<DishMedium> DishMedia { get; set; } = new List<DishMedium>();

    public virtual LookupValue DishStatusLv { get; set; } = null!;

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<PromotionRule> PromotionRules { get; set; } = new List<PromotionRule>();

    public virtual ICollection<PromotionTarget> PromotionTargets { get; set; } = new List<PromotionTarget>();

    public virtual ICollection<Recipe> Recipes { get; set; } = new List<Recipe>();
}
