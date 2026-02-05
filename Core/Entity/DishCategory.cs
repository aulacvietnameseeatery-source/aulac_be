using System;
using System.Collections.Generic;

namespace Core.Entity;

public partial class DishCategory
{
    public long CategoryId { get; set; }

    public string CategoryName { get; set; } = null!;

    public long? CategoryNameTextId { get; set; }

    public string? Description { get; set; }

    public long? DescriptionTextId { get; set; }

    public bool IsDisabled { get; set; }

    public virtual I18nText? CategoryNameText { get; set; }

    public virtual I18nText? DescriptionText { get; set; }

    public virtual ICollection<Dish> Dishes { get; set; } = new List<Dish>();

    public virtual ICollection<PromotionRule> PromotionRules { get; set; } = new List<PromotionRule>();

    public virtual ICollection<PromotionTarget> PromotionTargets { get; set; } = new List<PromotionTarget>();
}
