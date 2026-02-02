using System;
using System.Collections.Generic;

namespace Core.Entity;

public partial class PromotionTarget
{
    public long TargetId { get; set; }

    public long PromotionId { get; set; }

    public long? DishId { get; set; }

    public long? CategoryId { get; set; }

    public virtual DishCategory? Category { get; set; }

    public virtual Dish? Dish { get; set; }

    public virtual Promotion Promotion { get; set; } = null!;
}
