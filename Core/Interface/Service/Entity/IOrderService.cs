using Core.DTO.General;
using Core.DTO.Order;
using Core.Enum;

namespace Core.Interface.Service.Entity;

public interface IOrderService
{
	Task<PagedResultDTO<OrderHistoryDTO>> GetOrderHistoryAsync(OrderHistoryQueryDTO query, CancellationToken cancellationToken = default);
	Task<OrderStatusCountDTO> GetOrderStatusCountAsync(CancellationToken cancellationToken = default);
	Task<List<KitchenOrderDTO>> GetKitchenOrdersAsync(CancellationToken cancellationToken = default);
	Task UpdateOrderStatusAsync(long orderId, OrderStatusCode newStatus, CancellationToken cancellationToken = default);
	Task UpdateOrderItemStatusAsync(long orderItemId, uint newStatusLvId, string? rejectReason, CancellationToken cancellationToken = default);

	/// <summary>
	/// Cancels an order item if it hasn't been started yet (CREATED status only).
	/// </summary>
	Task CancelOrderItemAsync(long orderItemId, CancellationToken cancellationToken = default);

	/// <summary>Creates a new order from the customer-facing menu (QR flow).</summary>
	Task<CreateOrderResponseDTO> CreateOrderAsync(CreateOrderRequestDTO request, CancellationToken cancellationToken = default);

	/// <summary>
	/// Returns all orders placed at the given table today, grouped as rounds.
	/// Public endpoint – no authentication required.
	/// </summary>
	Task<CustomerOrderHistoryDTO> GetCustomerOrderHistoryAsync(string tableCode, CancellationToken cancellationToken = default);

	/// <summary>
	/// Appends more items to an existing order (same table / same customer session).
	/// </summary>
	Task AddItemsToOrderAsync(long orderId, AddOrderItemsRequestDTO request, CancellationToken cancellationToken = default);

	/// <summary>
	/// Returns history for a single specific order (customer-facing).
	/// </summary>
	Task<CustomerOrderHistoryDTO> GetCustomerOrderByIdAsync(long orderId, CancellationToken cancellationToken = default);

    Task<long> CreateOrderAsync(long staffId, CreateOrderRequest request, CancellationToken ct);
    Task<OrderHistoryDTO> GetOrderByIdAsync(long orderId, CancellationToken cancellationToken = default);
    Task AddItemsAsync(long orderId, AddOrderItemsRequest request, CancellationToken ct);

    Task<List<RecentOrderDTO>> GetRecentOrdersAsync(long userId, List<string> roles, int limit, CancellationToken ct);

}
