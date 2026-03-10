using Core.DTO.Order;
using Core.Entity;

namespace Core.Interface.Repo;

public interface ISaleInvoiceRepository
{
    Task<Order?> GetOrderForInvoiceAsync(long orderId, CancellationToken cancellationToken = default);
    Task<decimal> GetTotalDiscountAsync(long orderId, CancellationToken cancellationToken = default);
}
