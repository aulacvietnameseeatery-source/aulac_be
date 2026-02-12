namespace Core.DTO.DishCategory;

/// <summary>
/// Dish category list query parameters
/// </summary>
public class DishCategoryListQueryDTO
{
    /// <summary>
    /// Search by category name or description
    /// </summary>
    public string? Search { get; set; }
    
    /// <summary>
    /// Filter by disabled status (null = all, true = disabled only, false = enabled only)
    /// </summary>
    public bool? IsDisabled { get; set; }

    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
