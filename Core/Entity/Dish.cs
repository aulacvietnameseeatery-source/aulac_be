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

    public string? Description { get; set; }

    public string? Slogan { get; set; }

    public string? Note { get; set; }

    public int? Calories { get; set; }

    public string? ShortDescription { get; set; }

    public sbyte? DisplayOrder { get; set; }

    public bool? ChefRecommended { get; set; }

    public int? PrepTimeMinutes { get; set; }

    public int? CookTimeMinutes { get; set; }

    public bool? IsOnline { get; set; }

    public long? DescriptionTextId { get; set; }

    public long? SloganTextId { get; set; }

    public long? NoteTextId { get; set; }

    public long? ShortDescriptionTextId { get; set; }

    public long DishNameTextId { get; set; }

    public virtual DishCategory Category { get; set; } = null!;

    public virtual I18nText? DescriptionText { get; set; }

    public virtual ICollection<DishMedium> DishMedia { get; set; } = new List<DishMedium>();

    public virtual I18nText DishNameText { get; set; } = null!;

    public virtual LookupValue DishStatusLv { get; set; } = null!;

    public virtual I18nText? NoteText { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<PromotionRule> PromotionRules { get; set; } = new List<PromotionRule>();

    public virtual ICollection<PromotionTarget> PromotionTargets { get; set; } = new List<PromotionTarget>();

    public virtual ICollection<Recipe> Recipes { get; set; } = new List<Recipe>();

    public virtual I18nText? ShortDescriptionText { get; set; }

    public virtual I18nText? SloganText { get; set; }
}
