using Core.DTO.General;
using Core.DTO.Order;

namespace Core.Interface.Service.Entity;

public interface ISaleInvoiceService
{
    Task<SaleInvoiceDTO> GetSaleInvoiceDetailAsync(long orderId, CancellationToken cancellationToken = default);
    Task<PagedResultDTO<SaleInvoiceListDTO>> GetSaleInvoiceListAsync(SaleInvoiceListQueryDTO query, CancellationToken cancellationToken = default);
}
