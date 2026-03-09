using Core.DTO.Ingredient;

namespace Core.DTO.Supplier;

/// <summary>
/// Supplier detail response DTO including supplied ingredients
/// </summary>
public class SupplierDetailDto
{
    public long SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public List<IngredientDto> Ingredients { get; set; } = new();
}
