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
    Task AddAsync(Order order, CancellationToken ct);
    Task<OrderHistoryDTO> GetOrderByIdAsync(long orderId, CancellationToken cancellationToken = default);
    Task<Order?> GetByIdForUpdateAsync(long orderId, CancellationToken ct);
}
