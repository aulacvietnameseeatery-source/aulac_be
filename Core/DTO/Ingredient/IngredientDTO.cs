using Core.DTO.Supplier;

namespace Core.DTO.Ingredient;

public class IngredientDTO
{
    public long IngredientId { get; set; }
    public string IngredientName { get; set; } = null!;

    public uint UnitLvId { get; set; }
    public string? UnitName { get; set; }

    public uint? TypeLvId { get; set; }
    public string? TypeName { get; set; }
    public long? ImageId { get; set; }
    public string? ImageUrl { get; set; }

    public decimal QuantityOnHand { get; set; }
    public decimal MinStockLevel { get; set; }
    public DateTime? LastUpdatedAt { get; set; }

    // Suppliers Info
    public List<SupplierDto> Suppliers { get; set; } = new List<SupplierDto>();
}