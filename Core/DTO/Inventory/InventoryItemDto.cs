namespace Core.DTO.Inventory;

/// <summary>
/// DTO for inventory item list view (extends ingredient info with category + stock).
/// </summary>
public class InventoryItemDto
{
    public long IngredientId { get; set; }
    public string IngredientName { get; set; } = null!;

    public uint UnitLvId { get; set; }
    public string? UnitName { get; set; }

    public uint? TypeLvId { get; set; }
    public string? TypeName { get; set; }

    public uint? CategoryLvId { get; set; }
    public string? CategoryCode { get; set; }
    public string? CategoryName { get; set; }

    public long? ImageId { get; set; }
    public string? ImageUrl { get; set; }

    public decimal QuantityOnHand { get; set; }
    public decimal MinStockLevel { get; set; }
    public DateTime? LastUpdatedAt { get; set; }

    public bool IsLowStock => QuantityOnHand <= MinStockLevel && MinStockLevel > 0;
}
