using Core.DTO.General;
using Core.DTO.Inventory;
using Core.Entity;

namespace Core.Interface.Repo;

public interface IInventoryRepository
{
    // ── Inventory Items (Ingredients extended with category) ──
    Task<PagedResultDTO<InventoryItemDto>> GetItemsPagedAsync(GetInventoryItemsFilterRequest filter, CancellationToken ct = default);

    // ── Transactions ──
    Task<long> CreateTransactionAsync(InventoryTransaction transaction, List<InventoryTransactionItem> items, CancellationToken ct = default);
    Task<InventoryTransaction?> GetTransactionByIdAsync(long transactionId, CancellationToken ct = default);
    Task<PagedResultDTO<InventoryTransactionListDto>> GetTransactionsPagedAsync(GetTransactionsFilterRequest filter, CancellationToken ct = default);
    Task UpdateTransactionAsync(InventoryTransaction transaction, CancellationToken ct = default);

    // ── Stock ──
    Task<CurrentStock?> GetCurrentStockAsync(long ingredientId, CancellationToken ct = default);
    Task UpdateStockAsync(long ingredientId, decimal quantityChange, CancellationToken ct = default);
    Task<PagedResultDTO<StockCardDto>> GetStockCardAsync(long ingredientId, int pageIndex, int pageSize, CancellationToken ct = default);

    // ── Dashboard ──
    Task<InventoryDashboardDto> GetDashboardAsync(CancellationToken ct = default);

    // ── Transaction Code ──
    Task<int> GetTodayTransactionCountByTypeAsync(string typeCode, CancellationToken ct = default);
}
