using Core.DTO.General;
using Core.DTO.Order;
using Core.DTO.Shift;
using Core.Entity;

namespace Core.Interface.Repo;

public interface IOrderRepository
{
	Task<PagedResultDTO<OrderHistoryDTO>> GetOrderHistoryAsync(OrderHistoryQueryDTO query, CancellationToken cancellationToken = default);
	Task<OrderStatusCountDTO> GetOrderStatusCountAsync(
		uint pendingStatusId,
		uint inProgressStatusId,
		uint completedStatusId,
		uint cancelledStatusId,
		CancellationToken cancellationToken = default);

	Task<List<KitchenOrderDTO>> GetKitchenOrdersAsync(
		uint pendingStatusId,
		uint inProgressStatusId,
		uint completedStatusId,
		uint cancelledStatusId,
		CancellationToken cancellationToken = default);

	Task UpdateOrderItemStatusAsync(
		long orderItemId,
		uint newStatusLvId,
		string? rejectReason,
		uint inProgressItemStatusId,
		uint readyItemStatusId,
		uint servedItemStatusId,
		uint rejectedItemStatusId,
		uint cancelledItemStatusId,
		uint pendingOrderStatusId,
		uint inProgressOrderStatusId,
		uint completedOrderStatusId,
		uint cancelledOrderStatusId,
		uint availableTableStatusId,
		CancellationToken cancellationToken = default);


	/// <summary>Persists a new order together with its items. Returns the generated order_id.</summary>
	Task<long> CreateOrderAsync(Order order, List<OrderItem> items, CancellationToken cancellationToken = default);

	/// <summary>
	/// Returns all orders (grouped as rounds) placed at a given table today.
	/// Used by the public customer-facing history endpoint – no authentication required.
	/// </summary>
	Task<CustomerOrderHistoryDTO> GetCustomerOrderHistoryAsync(string tableCode, CancellationToken cancellationToken = default);

	/// <summary>
	/// Appends new items to an existing order and updates its total amount.
	/// </summary>
	Task AddItemsToOrderAsync(long orderId, List<OrderItem> items, uint completedOrderStatusId, uint cancelledOrderStatusId, uint pendingOrderStatusId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Returns history for a single specific order (used by customer history panel).
	/// </summary>
	Task<CustomerOrderHistoryDTO> GetCustomerOrderByIdAsync(long orderId, CancellationToken cancellationToken = default);

    Task AddAsync(Order order, CancellationToken ct);
    Task<OrderHistoryDTO> GetOrderByIdAsync(long orderId, CancellationToken cancellationToken = default);
    Task<Order?> GetByIdForUpdateAsync(long orderId, CancellationToken ct);
    Task<Order?> GetOrderWithItemsAsync(long orderId, CancellationToken ct);
    Task<Order?> GetActiveOrderByTableAsync(long tableId, CancellationToken ct);
    Task<OrderItem?> GetOrderItemAsync(long orderItemId, CancellationToken ct);

	Task<List<RecentOrderDTO>> GetRecentOrdersAsync(long userId, List<string> roles, int limit, CancellationToken ct);

	Task<List<ShiftLiveOrderSnapshotDto>> GetShiftLiveOrderSnapshotsAsync(
		DateTime fromUtc,
		DateTime toUtc,
		IEnumerable<long> staffIds,
		CancellationToken ct = default);

	Task<List<ShiftLiveIssueSnapshotDto>> GetShiftLiveIssueSnapshotsAsync(
		DateTime fromUtc,
		DateTime toUtc,
		IEnumerable<long> staffIds,
		CancellationToken ct = default);
}
