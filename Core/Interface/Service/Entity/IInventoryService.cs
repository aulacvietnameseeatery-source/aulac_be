using Core.DTO.General;
using Core.DTO.Inventory;

namespace Core.Interface.Service.Entity;

public interface IInventoryService
{
    // ── Items ──
    Task<PagedResultDTO<InventoryItemDto>> GetItemsAsync(GetInventoryItemsFilterRequest filter, CancellationToken ct = default);

    // ── Transactions ──
    Task<InventoryTransactionDetailDto> CreateTransactionAsync(CreateInventoryTransactionRequest request, long createdByUserId, CancellationToken ct = default);
    Task<InventoryTransactionDetailDto> GetTransactionDetailAsync(long transactionId, CancellationToken ct = default);
    Task<PagedResultDTO<InventoryTransactionListDto>> GetTransactionsAsync(GetTransactionsFilterRequest filter, CancellationToken ct = default);

    /// <summary>
    /// Submit a DRAFT transaction for approval → PENDING_APPROVAL.
    /// </summary>
    Task<InventoryTransactionDetailDto> SubmitTransactionAsync(long transactionId, SubmitTransactionRequest? request, long userId, CancellationToken ct = default);

    /// <summary>
    /// Approve or reject a PENDING_APPROVAL transaction.
    /// On approval → COMPLETED (stock updated). On rejection → CANCELLED.
    /// </summary>
    Task<InventoryTransactionDetailDto> ApproveTransactionAsync(long transactionId, ApproveTransactionRequest request, long approvedByUserId, CancellationToken ct = default);

    // ── Stock Card ──
    Task<PagedResultDTO<StockCardDto>> GetStockCardAsync(long ingredientId, int pageIndex, int pageSize, CancellationToken ct = default);

    // ── Dashboard ──
    Task<InventoryDashboardDto> GetDashboardAsync(CancellationToken ct = default);
}
