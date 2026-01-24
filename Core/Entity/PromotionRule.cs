using System;
using System.Collections.Generic;

namespace Core.Entity;

public partial class PromotionRule
{
    public long RuleId { get; set; }

    public long PromotionId { get; set; }

    public decimal? MinOrderValue { get; set; }

    public int? MinQuantity { get; set; }

    public long? RequiredDishId { get; set; }

    public long? RequiredCategoryId { get; set; }

    public virtual Promotion Promotion { get; set; } = null!;

    public virtual DishCategory? RequiredCategory { get; set; }

    public virtual Dish? RequiredDish { get; set; }
}
