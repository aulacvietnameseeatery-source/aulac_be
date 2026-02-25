using Core.DTO.General;
using Core.DTO.Order;

namespace Core.Interface.Service.Entity;

public interface IOrderService
{
	Task<PagedResultDTO<OrderHistoryDTO>> GetOrderHistoryAsync(OrderHistoryQueryDTO query, CancellationToken cancellationToken = default);
	Task<OrderStatusCountDTO> GetOrderStatusCountAsync(CancellationToken cancellationToken = default);

}
