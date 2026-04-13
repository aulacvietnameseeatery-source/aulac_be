using Core.DTO.General;
using Core.DTO.Inventory;

namespace Core.Interface.Service.Others
{
    public interface IInvoiceScanService
    {
        Task<InvoiceScanResult> ScanInvoiceAsync(MediaFileInput image, CancellationToken ct = default);
    }
}
