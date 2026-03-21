using Core.Enum;

namespace Core.DTO.Order;

public class SaleInvoiceListQueryDTO
{
    public string? Search { get; set; }        // search by InvoiceCode, CustomerName, StaffName, TableCode
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 10;
  
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? OrderStatusCode { get; set; }
}
