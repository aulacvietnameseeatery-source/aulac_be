using Core.DTO.General;
using Core.DTO.Order;
using Core.Interface.Repo;
using Core.Interface.Service.Entity;

namespace Core.Service;

public class OrderService : IOrderService
{
	private readonly IOrderRepository _orderRepository;

	public OrderService(IOrderRepository orderRepository)
	{
		_orderRepository = orderRepository;
	}

	public Task<PagedResultDTO<OrderHistoryDTO>> GetOrderHistoryAsync(OrderHistoryQueryDTO query, CancellationToken cancellationToken = default)
		=> _orderRepository.GetOrderHistoryAsync(query, cancellationToken);

	public Task<OrderStatusCountDTO> GetOrderStatusCountAsync(CancellationToken cancellationToken = default)
		=> _orderRepository.GetOrderStatusCountAsync(cancellationToken);

	public Task<List<KitchenOrderDTO>> GetKitchenOrdersAsync(CancellationToken cancellationToken = default)
		=> _orderRepository.GetKitchenOrdersAsync(cancellationToken);

	public Task UpdateOrderItemStatusAsync(long orderItemId, uint newStatusLvId, string? rejectReason, CancellationToken cancellationToken = default)
		=> _orderRepository.UpdateOrderItemStatusAsync(orderItemId, newStatusLvId, rejectReason, cancellationToken);
}

