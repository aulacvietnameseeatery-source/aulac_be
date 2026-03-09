namespace Core.DTO.Ingredient;

/// <summary>
/// Ingredient response DTO
/// </summary>
public class IngredientDto
{
    public long IngredientId { get; set; }
    public string IngredientName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
}
