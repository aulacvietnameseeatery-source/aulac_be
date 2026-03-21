using Core.DTO.General;
using Core.DTO.Order;
using Core.Exceptions;
using Core.Interface.Repo;
using Core.Interface.Service.Entity;

namespace Core.Service;

public class SaleInvoiceService : ISaleInvoiceService
{
    private readonly ISaleInvoiceRepository _saleInvoiceRepository;

    public SaleInvoiceService(ISaleInvoiceRepository saleInvoiceRepository)
    {
        _saleInvoiceRepository = saleInvoiceRepository;
    }

    public async Task<SaleInvoiceDTO> GetSaleInvoiceDetailAsync(long orderId, CancellationToken cancellationToken = default)
    {
        var order = await _saleInvoiceRepository.GetOrderForInvoiceAsync(orderId, cancellationToken);
        
        if (order == null)
            throw new NotFoundException($"Order with id {orderId} was not found.");

        var promotions = await _saleInvoiceRepository.GetTotalDiscountAsync(orderId, cancellationToken);

        var invoice = new SaleInvoiceDTO
        {
            OrderId = order.OrderId,
            InvoiceCode = "#INV" + order.OrderId.ToString("D4"),
            CreatedAt = order.CreatedAt,
            OrderType = order.SourceLv?.ValueName ?? "Unknown",
            TableCode = order.Table?.TableCode ?? "",
            StaffName = order.Staff?.FullName ?? "",
            CustomerName = order.Customer?.FullName ?? "",
            CustomerPhone = order.Customer?.Phone ?? "",
            IsPaid = order.Payments.Any(),
            PaymentMethod = order.Payments.Any() ? (order.Payments.FirstOrDefault()?.MethodLv?.ValueName ?? "-") : "-",
            Items = order.OrderItems
                .Where(oi => oi.ItemStatusLv.ValueCode != "REJECTED" && oi.ItemStatusLv.ValueCode != "CANCELLED")
                .Select(oi => new SaleInvoiceItemDTO
            {
                OrderItemId = oi.OrderItemId,
                Quantity = oi.Quantity,
                ItemName = oi.Dish?.DishName ?? "",
                ItemPrice = oi.Price,
                Amount = oi.Price * oi.Quantity,
                Note = oi.Note
            }).ToList()
        };

        invoice.SubTotal = invoice.Items.Sum(i => i.Amount);
        invoice.DiscountAmount = promotions;
        invoice.TipAmount = order.TipAmount ?? 0;
        invoice.TotalAmount = invoice.SubTotal - invoice.DiscountAmount + invoice.TipAmount;

        return invoice;
    }

    public async Task<PagedResultDTO<SaleInvoiceListDTO>> GetSaleInvoiceListAsync(SaleInvoiceListQueryDTO query, CancellationToken cancellationToken = default)
    {
        return await _saleInvoiceRepository.GetOrdersForInvoiceListAsync(query, cancellationToken);
    }
}
