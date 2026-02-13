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
}
