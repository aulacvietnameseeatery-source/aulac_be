using Core.DTO.Order;

namespace Core.Interface.Service.Entity;

public interface ISaleInvoiceService
{
    Task<SaleInvoiceDTO> GetSaleInvoiceDetailAsync(long orderId, CancellationToken cancellationToken = default);
}
