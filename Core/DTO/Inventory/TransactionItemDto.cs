namespace Core.DTO.Inventory;

/// <summary>
/// DTO for a transaction line item.
/// </summary>
public class TransactionItemDto
{
    public long TransactionItemId { get; set; }

    public long IngredientId { get; set; }
    public string? IngredientName { get; set; }
    public string? IngredientImageUrl { get; set; }

    public uint? CategoryLvId { get; set; }
    public string? CategoryCode { get; set; }
    public string? CategoryName { get; set; }

    public decimal Quantity { get; set; }

    public uint UnitLvId { get; set; }
    public string? UnitName { get; set; }

    public decimal? UnitPrice { get; set; }

    /// <summary>
    /// Stock-check: the system quantity before counting.
    /// </summary>
    public decimal? SystemQuantity { get; set; }

    /// <summary>
    /// Stock-check: the actual counted quantity.
    /// </summary>
    public decimal? ActualQuantity { get; set; }

    public uint? VarianceReasonLvId { get; set; }
    public string? VarianceReasonName { get; set; }

    public string? Note { get; set; }
}
