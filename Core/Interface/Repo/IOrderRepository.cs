using Core.DTO.General;
using Core.DTO.Order;
using Core.Entity;

namespace Core.Interface.Repo;

public interface IOrderRepository
{
	Task<PagedResultDTO<OrderHistoryDTO>> GetOrderHistoryAsync(OrderHistoryQueryDTO query, CancellationToken cancellationToken = default);
	Task<OrderStatusCountDTO> GetOrderStatusCountAsync(CancellationToken cancellationToken = default);
	Task<List<KitchenOrderDTO>> GetKitchenOrdersAsync(CancellationToken cancellationToken = default);
	Task UpdateOrderItemStatusAsync(long orderItemId, uint newStatusLvId, string? rejectReason, CancellationToken cancellationToken = default);

	/// <summary>Persists a new order together with its items. Returns the generated order_id.</summary>
	Task<long> CreateOrderAsync(Order order, List<OrderItem> items, CancellationToken cancellationToken = default);

	/// <summary>
	/// Returns all orders (grouped as rounds) placed at a given table today.
	/// Used by the public customer-facing history endpoint – no authentication required.
	/// </summary>
	Task<CustomerOrderHistoryDTO> GetCustomerOrderHistoryAsync(string tableCode, CancellationToken cancellationToken = default);
}
