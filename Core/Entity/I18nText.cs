using System;
using System.Collections.Generic;

namespace Core.Entity;

public partial class I18nText
{
    public long TextId { get; set; }

    public string TextKey { get; set; } = null!;

    public string SourceLangCode { get; set; } = null!;

    public string SourceText { get; set; } = null!;

    public string? Context { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<DishCategory> DishCategoryCategoryNameTexts { get; set; } = new List<DishCategory>();

    public virtual ICollection<DishCategory> DishCategoryDescriptionTexts { get; set; } = new List<DishCategory>();

    public virtual ICollection<Dish> DishDescriptionTexts { get; set; } = new List<Dish>();

    public virtual ICollection<Dish> DishDishNameTexts { get; set; } = new List<Dish>();

    public virtual ICollection<Dish> DishNoteTexts { get; set; } = new List<Dish>();

    public virtual ICollection<Dish> DishShortDescriptionTexts { get; set; } = new List<Dish>();

    public virtual ICollection<Dish> DishSloganTexts { get; set; } = new List<Dish>();

    public virtual ICollection<I18nTranslation> I18nTranslations { get; set; } = new List<I18nTranslation>();

    public virtual ICollection<Ingredient> Ingredients { get; set; } = new List<Ingredient>();

    public virtual ICollection<LookupType> LookupTypeTypeDescTexts { get; set; } = new List<LookupType>();

    public virtual ICollection<LookupType> LookupTypeTypeNameTexts { get; set; } = new List<LookupType>();

    public virtual ICollection<LookupValue> LookupValueValueDescTexts { get; set; } = new List<LookupValue>();

    public virtual ICollection<LookupValue> LookupValueValueNameTexts { get; set; } = new List<LookupValue>();

    public virtual ICollection<Promotion> PromotionPromoDescTexts { get; set; } = new List<Promotion>();

    public virtual ICollection<Promotion> PromotionPromoNameTexts { get; set; } = new List<Promotion>();

    public virtual ICollection<ServiceErrorCategory> ServiceErrorCategoryCategoryDescTexts { get; set; } = new List<ServiceErrorCategory>();

    public virtual ICollection<ServiceErrorCategory> ServiceErrorCategoryCategoryNameTexts { get; set; } = new List<ServiceErrorCategory>();

    public virtual I18nLanguage SourceLangCodeNavigation { get; set; } = null!;
}
