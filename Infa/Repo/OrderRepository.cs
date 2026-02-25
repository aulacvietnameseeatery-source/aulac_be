using Core.DTO.General;
using Core.DTO.Order;
using Core.Interface.Repo;
using Infa.Data;
using Microsoft.EntityFrameworkCore;

namespace Infa.Repo;

public class OrderRepository : IOrderRepository
{
	private readonly RestaurantMgmtContext _context;

	public OrderRepository(RestaurantMgmtContext context)
	{
		_context = context;
	}

	public async Task<PagedResultDTO<OrderHistoryDTO>> GetOrderHistoryAsync(OrderHistoryQueryDTO query, CancellationToken cancellationToken = default)
	{
		var queryable = _context.Orders
			.Include(o => o.OrderStatusLv)
			.Include(o => o.SourceLv)
			.Include(o => o.Customer)
			.Include(o => o.Staff)
			.Include(o => o.Table)
			.Include(o => o.OrderItems)
			.AsQueryable();

		// Filter by order status
		if (query.OrderStatusLvId.HasValue)
		{
			queryable = queryable.Where(o => o.OrderStatusLvId == query.OrderStatusLvId.Value);
		}

		// Filter by search term (OrderId, CustomerName, StaffName, TableCode)
		if (!string.IsNullOrWhiteSpace(query.Search))
		{
			var searchTerm = query.Search.Trim().ToLower();
			queryable = queryable.Where(o =>
				o.OrderId.ToString().Contains(searchTerm) ||
				(o.Customer != null && o.Customer.FullName != null && o.Customer.FullName.ToLower().Contains(searchTerm)) ||
				o.Staff.FullName.ToLower().Contains(searchTerm) ||
				o.Table.TableCode.ToLower().Contains(searchTerm)
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

		var totalCount = await queryable.CountAsync(cancellationToken);

		var pageIndex = query.PageIndex < 1 ? 1 : query.PageIndex;
		var pageSize = query.PageSize < 1 ? 10 : query.PageSize;

		var orders = await queryable
			.OrderByDescending(o => o.CreatedAt)
			.Skip((pageIndex - 1) * pageSize)
			.Take(pageSize)
			.Select(o => new OrderHistoryDTO
			{
				OrderId = o.OrderId,
				TableId = o.TableId,
				TableCode = o.Table.TableCode,
				StaffId = o.StaffId,
				StaffName = o.Staff.FullName,
				CustomerId = o.CustomerId,
				CustomerName = o.Customer.FullName,
				TotalAmount = o.TotalAmount,
				TipAmount = o.TipAmount,
				OrderStatus = o.OrderStatusLv.ValueName,
				Source = o.SourceLv.ValueName,
				CreatedAt = o.CreatedAt,
				UpdatedAt = o.UpdatedAt,
				OrderItems = o.OrderItems.Select(oi => new OrderItemDTO
				{
					OrderItemId = oi.OrderItemId,
					DishId = oi.DishId,
					DishName = oi.Dish.DishName,
					Quantity = oi.Quantity,
					Price = oi.Price,
					ItemStatus = oi.ItemStatusLv.ValueName,
					RejectReason = oi.RejectReason
				}).ToList()
			})
			.ToListAsync(cancellationToken);

		return new PagedResultDTO<OrderHistoryDTO>
		{
			PageData = orders,
			TotalCount = totalCount,
			PageIndex = pageIndex,
			PageSize = pageSize
		};
	}
	public async Task<OrderStatusCountDTO> GetOrderStatusCountAsync(CancellationToken cancellationToken = default)
	{
		// Single query: group by statusLvId and count
		var counts = await _context.Orders
			.GroupBy(o => o.OrderStatusLvId)
			.Select(g => new { StatusLvId = g.Key, Count = g.Count() })
			.ToListAsync(cancellationToken);

		var dict = counts.ToDictionary(c => c.StatusLvId, c => c.Count);

		const uint pendingId    = 28;
		const uint inProgressId = 29;
		const uint completedId  = 30;
		const uint cancelledId  = 31;

		int pending    = dict.GetValueOrDefault(pendingId,    0);
		int inProgress = dict.GetValueOrDefault(inProgressId, 0);
		int completed  = dict.GetValueOrDefault(completedId,  0);
		int cancelled  = dict.GetValueOrDefault(cancelledId,  0);

		return new OrderStatusCountDTO
		{
			All        = pending + inProgress + completed + cancelled,
			Pending    = pending,
			InProgress = inProgress,
			Completed  = completed,
			Cancelled  = cancelled,
		};
	}
}
