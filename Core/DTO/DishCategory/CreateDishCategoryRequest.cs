using System.ComponentModel.DataAnnotations;

namespace Core.DTO.DishCategory;

/// <summary>
/// Request DTO for creating a new dish category.
/// I18n dictionary keys are language codes ("en", "vi", "fr").
/// </summary>
public class CreateDishCategoryRequest 
{
    [Required]
    public Dictionary<string, CategoryI18nDto> I18n { get; set; } = new();

    public bool IsDisabled { get; set; } = false;

}
