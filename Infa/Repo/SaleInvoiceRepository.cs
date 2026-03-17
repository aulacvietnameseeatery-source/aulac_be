using Core.DTO.General;
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

    public async Task<PagedResultDTO<SaleInvoiceListDTO>> GetOrdersForInvoiceListAsync(SaleInvoiceListQueryDTO query, CancellationToken cancellationToken = default)
    {
        var queryable = _context.Orders
            .AsNoTracking()
            .Include(o => o.OrderStatusLv)
            .Include(o => o.Table)
            .Include(o => o.Staff)
            .Include(o => o.Customer)
            .Include(o => o.Payments)
                .ThenInclude(p => p.MethodLv)
            .Include(o => o.OrderItems)
            .Include(o => o.OrderPromotions)
            .AsQueryable();

        // Filter by search (InvoiceCode, CustomerName, StaffName, TableCode)
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var searchTerm = query.Search.Trim().ToLower();
            
            // Extract numeric part if search starts with #INV
            long? invoiceId = null;
            if (searchTerm.StartsWith("#inv"))
            {
                var numericPart = searchTerm.Substring(4);
                if (long.TryParse(numericPart, out var parsedId))
                {
                    invoiceId = parsedId;
                }
            }
            
            queryable = queryable.Where(o =>
                (invoiceId.HasValue && o.OrderId == invoiceId.Value) ||
                o.OrderId.ToString().Contains(searchTerm) ||
                (o.Customer != null && (o.Customer.FullName ?? "").ToLower().Contains(searchTerm)) ||
                (o.Staff != null && (o.Staff.FullName ?? "").ToLower().Contains(searchTerm)) ||
                (o.Table != null && (o.Table.TableCode ?? "").ToLower().Contains(searchTerm))
            );
        }

        // Filter by date range
        if (query.FromDate.HasValue)
        {
            queryable = queryable.Where(o => o.CreatedAt >= query.FromDate.Value);
        }
        if (query.ToDate.HasValue)
        {
            queryable = queryable.Where(o => o.CreatedAt <= query.ToDate.Value);
        }

        // Get total count after filters
        var totalCount = await queryable.CountAsync(cancellationToken);

        // Validate pagination parameters
        var pageIndex = query.PageIndex < 1 ? 1 : query.PageIndex;
        var pageSize = query.PageSize < 1 ? 10 : query.PageSize;

        // Apply pagination and ordering, then map to DTO
        var invoiceList = await queryable
            .OrderByDescending(o => o.CreatedAt)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new SaleInvoiceListDTO
            {
                OrderId = o.OrderId,
                InvoiceCode = "#INV" + o.OrderId.ToString("D4"),
                CreatedAt = o.CreatedAt,
                TableCode = o.Table != null ? (o.Table.TableCode ?? "") : "",
                StaffName = o.Staff != null ? (o.Staff.FullName ?? "") : "",
                CustomerName = o.Customer != null ? (o.Customer.FullName ?? "") : "",
                TotalAmount = o.TotalAmount - o.OrderPromotions.Sum(op => op.DiscountAmount),
                TipAmount = o.TipAmount ?? 0,
                OrderStatus = o.OrderStatusLv != null ? (o.OrderStatusLv.ValueName ?? "") : "",
                PaymentMethod = o.Payments.Any() ? (o.Payments.FirstOrDefault()!.MethodLv!.ValueName ?? "-") : "-"
            })
            .ToListAsync(cancellationToken);

        return new PagedResultDTO<SaleInvoiceListDTO>
        {
            PageData = invoiceList,
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}
