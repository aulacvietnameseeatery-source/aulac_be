using System.ComponentModel.DataAnnotations;

namespace Core.DTO.DishCategory;

/// <summary>
/// Multilingual content for a single language of a dish category
/// </summary>
public class CategoryI18nDto
{
    [MaxLength(100, ErrorMessage = "Category name cannot exceed 100 characters.")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100, ErrorMessage = "Description cannot exceed 100 characters.")]
    public string? Description { get; set; }
}
