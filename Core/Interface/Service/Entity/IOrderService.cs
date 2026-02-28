using Core.DTO.General;
using Core.DTO.Order;

namespace Core.Interface.Service.Entity;

public interface IOrderService
{
	Task<PagedResultDTO<OrderHistoryDTO>> GetOrderHistoryAsync(OrderHistoryQueryDTO query, CancellationToken cancellationToken = default);
	Task<OrderStatusCountDTO> GetOrderStatusCountAsync(CancellationToken cancellationToken = default);
	Task<List<KitchenOrderDTO>> GetKitchenOrdersAsync(CancellationToken cancellationToken = default);
	Task UpdateOrderItemStatusAsync(long orderItemId, uint newStatusLvId, string? rejectReason, CancellationToken cancellationToken = default);

	/// <summary>Creates a new order from the customer-facing menu (QR flow).</summary>
	Task<CreateOrderResponseDTO> CreateOrderAsync(CreateOrderRequestDTO request, CancellationToken cancellationToken = default);

	/// <summary>
	/// Returns all orders placed at the given table today, grouped as rounds.
	/// Public endpoint – no authentication required.
	/// </summary>
	Task<CustomerOrderHistoryDTO> GetCustomerOrderHistoryAsync(string tableCode, CancellationToken cancellationToken = default);
}
