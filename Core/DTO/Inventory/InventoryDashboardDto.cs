namespace Core.DTO.Inventory;

/// <summary>
/// Dashboard summary for the inventory module.
/// </summary>
public class InventoryDashboardDto
{
    public int TotalItems { get; set; }
    public int LowStockItems { get; set; }
    public int OutOfStockItems { get; set; }
    public int PendingTransactions { get; set; }

    public List<LowStockItemDto> LowStockList { get; set; } = new();
    public List<RecentTransactionDto> RecentTransactions { get; set; } = new();
}

public class LowStockItemDto
{
    public long IngredientId { get; set; }
    public string? IngredientName { get; set; }
    public string? CategoryName { get; set; }
    public decimal QuantityOnHand { get; set; }
    public decimal MinStockLevel { get; set; }
    public string? UnitName { get; set; }
}

public class RecentTransactionDto
{
    public long TransactionId { get; set; }
    public string? TransactionCode { get; set; }
    public string? TypeName { get; set; }
    public string? StatusName { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime? CreatedAt { get; set; }
    public int ItemCount { get; set; }
}
