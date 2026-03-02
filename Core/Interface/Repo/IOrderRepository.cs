using Core.DTO.General;
using Core.DTO.Order;

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
		CancellationToken cancellationToken = default);

	Task UpdateOrderItemStatusAsync(
		long orderItemId,
		uint newStatusLvId,
		string? rejectReason,
		uint inProgressItemStatusId,
		uint servedItemStatusId,
		uint rejectedItemStatusId,
		uint pendingOrderStatusId,
		uint inProgressOrderStatusId,
		uint completedOrderStatusId,
		uint cancelledOrderStatusId,
		CancellationToken cancellationToken = default);

}
