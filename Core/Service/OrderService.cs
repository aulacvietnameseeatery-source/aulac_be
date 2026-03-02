using Core.DTO.General;
using Core.DTO.Order;
using Core.Enum;
using Core.Extensions;
using Core.Interface.Repo;
using Core.Interface.Service.Entity;

namespace Core.Service;

public class OrderService : IOrderService
{
	private readonly IOrderRepository _orderRepository;
	private readonly ILookupResolver _lookupResolver;

	public OrderService(IOrderRepository orderRepository, ILookupResolver lookupResolver)
	{
		_orderRepository = orderRepository;
		_lookupResolver = lookupResolver;
	}

	public Task<PagedResultDTO<OrderHistoryDTO>> GetOrderHistoryAsync(OrderHistoryQueryDTO query, CancellationToken cancellationToken = default)
		=> _orderRepository.GetOrderHistoryAsync(query, cancellationToken);

	public async Task<OrderStatusCountDTO> GetOrderStatusCountAsync(CancellationToken cancellationToken = default)
	{
		var pendingId    = await OrderStatusCode.PENDING.ToOrderStatusIdAsync(_lookupResolver, cancellationToken);
		var inProgressId = await OrderStatusCode.IN_PROGRESS.ToOrderStatusIdAsync(_lookupResolver, cancellationToken);
		var completedId  = await OrderStatusCode.COMPLETED.ToOrderStatusIdAsync(_lookupResolver, cancellationToken);
		var cancelledId  = await OrderStatusCode.CANCELLED.ToOrderStatusIdAsync(_lookupResolver, cancellationToken);

		return await _orderRepository.GetOrderStatusCountAsync(
			pendingId,
			inProgressId,
			completedId,
			cancelledId,
			cancellationToken);
	}

	public async Task<List<KitchenOrderDTO>> GetKitchenOrdersAsync(CancellationToken cancellationToken = default)
	{
		var pendingId    = await OrderStatusCode.PENDING.ToOrderStatusIdAsync(_lookupResolver, cancellationToken);
		var inProgressId = await OrderStatusCode.IN_PROGRESS.ToOrderStatusIdAsync(_lookupResolver, cancellationToken);

		return await _orderRepository.GetKitchenOrdersAsync(
			pendingId,
			inProgressId,
			cancellationToken);
	}

	public async Task UpdateOrderItemStatusAsync(long orderItemId, uint newStatusLvId, string? rejectReason, CancellationToken cancellationToken = default)
	{
		var inProgressItemId = await OrderItemStatusCode.IN_PROGRESS.ToOrderItemStatusIdAsync(_lookupResolver, cancellationToken);
		var servedItemId     = await OrderItemStatusCode.SERVED.ToOrderItemStatusIdAsync(_lookupResolver, cancellationToken);
		var rejectedItemId   = await OrderItemStatusCode.REJECTED.ToOrderItemStatusIdAsync(_lookupResolver, cancellationToken);

		var pendingOrderId    = await OrderStatusCode.PENDING.ToOrderStatusIdAsync(_lookupResolver, cancellationToken);
		var inProgressOrderId = await OrderStatusCode.IN_PROGRESS.ToOrderStatusIdAsync(_lookupResolver, cancellationToken);
		var completedOrderId  = await OrderStatusCode.COMPLETED.ToOrderStatusIdAsync(_lookupResolver, cancellationToken);
		var cancelledOrderId  = await OrderStatusCode.CANCELLED.ToOrderStatusIdAsync(_lookupResolver, cancellationToken);

		await _orderRepository.UpdateOrderItemStatusAsync(
			orderItemId,
			newStatusLvId,
			rejectReason,
			inProgressItemId,
			servedItemId,
			rejectedItemId,
			pendingOrderId,
			inProgressOrderId,
			completedOrderId,
			cancelledOrderId,
			cancellationToken);
	}
}

