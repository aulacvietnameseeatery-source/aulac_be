namespace Core.DTO.Order;

public class SaleInvoiceListDTO
{
    public long OrderId { get; set; }
    public string InvoiceCode { get; set; } = string.Empty;
    public DateTime? CreatedAt { get; set; }
    public string TableCode { get; set; } = string.Empty;
    public string StaffName { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal? TipAmount { get; set; }
    public string OrderStatus { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
}
