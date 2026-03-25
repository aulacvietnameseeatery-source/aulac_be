namespace Core.DTO.Inventory;

/// <summary>
/// Filter/query parameters for listing inventory items (ingredients).
/// Used by GET /api/inventory/items.
/// </summary>
public class GetInventoryItemsFilterRequest
{
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Searches by item name.
    /// </summary>
    public string? Search { get; set; }

    /// <summary>
    /// Filter by inventory category (FOOD_INGREDIENT, KITCHEN_TOOL, etc.).
    /// </summary>
    public uint? CategoryLvId { get; set; }

    /// <summary>
    /// Filter by item type within the category (e.g. KitchenToolType).
    /// </summary>
    public uint? TypeLvId { get; set; }

    /// <summary>
    /// Filter by low stock only.
    /// </summary>
    public bool? IsLowStock { get; set; }
}
