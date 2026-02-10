using System.ComponentModel.DataAnnotations;

namespace Core.DTO.DishCategory;

/// <summary>
/// Request DTO for creating a new dish category
/// </summary>
public class CreateDishCategoryRequest
{
    [Required(ErrorMessage = "Category name is required")]
    [StringLength(100, ErrorMessage = "Category name cannot exceed 200 characters")]
    public string CategoryName { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string? Description { get; set; }

    public bool IsDisabled { get; set; } = false;
}
