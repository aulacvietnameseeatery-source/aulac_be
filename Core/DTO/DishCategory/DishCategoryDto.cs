using Core.DTO.Dish;

namespace Core.DTO.DishCategory;

/// <summary>
/// Dish category response DTO
/// </summary>
public class DishCategoryDto
{
    public long CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsDisabled { get; set; }

    /// <summary>Translated names for vi / en / fr</summary>
    public I18nTextDto NameI18n { get; set; } = new();

    /// <summary>Translated descriptions for vi / en / fr</summary>
    public I18nTextDto DescriptionI18n { get; set; } = new();
}
