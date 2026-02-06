namespace Core.DTO.Dish;

/// <summary>
/// Recipe composition item DTO
/// </summary>
public class RecipeItemDto
{
    public long IngredientId { get; set; }
    public string IngredientName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? Note { get; set; }
}
