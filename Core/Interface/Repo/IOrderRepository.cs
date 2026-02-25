using Core.DTO.General;
using Core.DTO.Order;

namespace Core.Interface.Repo;

public interface IOrderRepository
{
	Task<PagedResultDTO<OrderHistoryDTO>> GetOrderHistoryAsync(OrderHistoryQueryDTO query, CancellationToken cancellationToken = default);
	Task<OrderStatusCountDTO> GetOrderStatusCountAsync(CancellationToken cancellationToken = default);

}
