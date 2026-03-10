using Core.DTO.Order;
using Core.Entity;
using Core.Exceptions;
using Core.Interface.Repo;
using Infa.Data;
using Microsoft.EntityFrameworkCore;

namespace Infa.Repo;

public class SaleInvoiceRepository : ISaleInvoiceRepository
{
    private readonly RestaurantMgmtContext _context;

    public SaleInvoiceRepository(RestaurantMgmtContext context)
    {
        _context = context;
    }

    public async Task<Order?> GetOrderForInvoiceAsync(long orderId, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .AsNoTracking()
            .Include(o => o.SourceLv)
            .Include(o => o.Table)
            .Include(o => o.Staff)
            .Include(o => o.Customer)
            .Include(o => o.Payments)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Dish)
            .FirstOrDefaultAsync(o => o.OrderId == orderId, cancellationToken);
    }

    public async Task<decimal> GetTotalDiscountAsync(long orderId, CancellationToken cancellationToken = default)
    {
        return await _context.OrderPromotions
            .Where(op => op.OrderId == orderId)
            .SumAsync(op => (decimal?)op.DiscountAmount, cancellationToken) ?? 0m;
    }
}
