using Core.DTO.General;
using Core.DTO.Order;
using Core.Entity;
using Core.Exceptions;
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
			.Include(o => o.Payments)
			.AsQueryable();

		// Filter by order status
		if (query.OrderStatusCode.HasValue)
		{
			var statusCode = query.OrderStatusCode.Value.ToString();
			queryable = queryable.Where(o => o.OrderStatusLv.ValueCode == statusCode);
		}
		else if (query.OrderStatusLvId.HasValue)
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
				IsPaid = o.Payments.Any(),
				OrderItems = o.OrderItems.Select(oi => new OrderItemDTO
				{
					OrderItemId = oi.OrderItemId,
					DishId = oi.DishId,
					DishName = oi.Dish.DishName,
					Quantity = oi.Quantity,
					Price = oi.Price,
					ItemStatus = oi.ItemStatusLv.ValueName,
					RejectReason = oi.RejectReason,
					Note = oi.Note
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
	public async Task<OrderStatusCountDTO> GetOrderStatusCountAsync(
		uint pendingStatusId,
		uint inProgressStatusId,
		uint completedStatusId,
		uint cancelledStatusId,
		CancellationToken cancellationToken = default)
	{
		// Single query: group by statusLvId and count
		var counts = await _context.Orders
			.GroupBy(o => o.OrderStatusLvId)
			.Select(g => new { StatusLvId = g.Key, Count = g.Count() })
			.ToListAsync(cancellationToken);

		var dict = counts.ToDictionary(c => c.StatusLvId, c => c.Count);

		int pending    = dict.GetValueOrDefault(pendingStatusId,    0);
		int inProgress = dict.GetValueOrDefault(inProgressStatusId, 0);
		int completed  = dict.GetValueOrDefault(completedStatusId,  0);
		int cancelled  = dict.GetValueOrDefault(cancelledStatusId,  0);

		return new OrderStatusCountDTO
		{
			All        = pending + inProgress + completed + cancelled,
			Pending    = pending,
			InProgress = inProgress,
			Completed  = completed,
			Cancelled  = cancelled,
		};
	}

	public async Task<List<KitchenOrderDTO>> GetKitchenOrdersAsync(
		uint pendingStatusId,
		uint inProgressStatusId,
		uint completedStatusId,
		uint cancelledStatusId,
		CancellationToken cancellationToken = default)
	{
		var today = DateTime.UtcNow.Date;
		var tomorrow = today.AddDays(1);
		var orders = await _context.Orders
			.Include(o => o.OrderStatusLv)
			.Include(o => o.Table)
			.Include(o => o.OrderItems)
				.ThenInclude(oi => oi.Dish)
			.Include(o => o.OrderItems)
				.ThenInclude(oi => oi.ItemStatusLv)
			.Where(o =>
				o.CreatedAt >= today &&
				o.CreatedAt < tomorrow &&
				(o.OrderStatusLvId == pendingStatusId ||
				 o.OrderStatusLvId == inProgressStatusId ||
				 o.OrderStatusLvId == completedStatusId ||
				 o.OrderStatusLvId == cancelledStatusId))
			.OrderBy(o => o.CreatedAt)
			.Select(o => new KitchenOrderDTO
			{
				OrderId = o.OrderId,
				TableCode = o.Table.TableCode,
				OrderStatus = o.OrderStatusLv.ValueCode,
				CreatedAt = o.CreatedAt,
				Items = o.OrderItems.Select(oi => new KitchenOrderItemDTO
				{
					OrderItemId = oi.OrderItemId,
					DishName = oi.Dish.DishName,
					Quantity = oi.Quantity,
					ItemStatus = oi.ItemStatusLv.ValueCode,
					Note = oi.Note,
					RejectReason = oi.RejectReason
				}).ToList()
			})
			.ToListAsync(cancellationToken);

		return orders;
	}

	public async Task UpdateOrderItemStatusAsync(
		long orderItemId,
		uint newStatusLvId,
		string? rejectReason,
		uint inProgressItemStatusId,
		uint readyItemStatusId,
		uint servedItemStatusId,
		uint rejectedItemStatusId,
		uint pendingOrderStatusId,
		uint inProgressOrderStatusId,
		uint completedOrderStatusId,
		uint cancelledOrderStatusId,
		uint availableTableStatusId,
		CancellationToken cancellationToken = default)
	{
		var item = await _context.OrderItems
			.FirstOrDefaultAsync(oi => oi.OrderItemId == orderItemId, cancellationToken)
			?? throw new KeyNotFoundException($"OrderItem {orderItemId} not found");

		item.ItemStatusLvId = newStatusLvId;
		item.RejectReason = rejectReason;

		await _context.SaveChangesAsync(cancellationToken);

		// ─── Auto-update Order Status based on Items ──────────────────────

		var order = await _context.Orders
			.Include(o => o.Table)
			.FirstOrDefaultAsync(o => o.OrderId == item.OrderId, cancellationToken);

		if (order != null)
		{
			bool orderStatusChanged = false;

			// 1. Subtract rejected/cancelled item from total amount
			if (newStatusLvId == rejectedItemStatusId)
			{
				order.TotalAmount -= item.Price * item.Quantity;
				orderStatusChanged = true;
			}

			// 2. Move from PENDING to IN_PROGRESS if an item starts
			if (newStatusLvId == inProgressItemStatusId && order.OrderStatusLvId == pendingOrderStatusId)
			{
				order.OrderStatusLvId = inProgressOrderStatusId;
				orderStatusChanged = true;
			}

			// 3. Auto-complete or auto-cancel when ALL items are finished
			var allItems = await _context.OrderItems
				.Where(oi => oi.OrderId == item.OrderId)
				.Select(oi => oi.ItemStatusLvId)
				.ToListAsync(cancellationToken);

			bool allFinished = allItems.All(lvId => lvId == servedItemStatusId || lvId == readyItemStatusId || lvId == rejectedItemStatusId);

			if (allFinished)
			{
				bool hasBeenWorked = allItems.Any(lvId => lvId == servedItemStatusId || lvId == readyItemStatusId);
				uint targetStatusId = hasBeenWorked ? completedOrderStatusId : cancelledOrderStatusId;

				if (order.OrderStatusLvId != targetStatusId)
				{
					order.OrderStatusLvId = targetStatusId;
					orderStatusChanged = true;

					// If order moves to CANCELLED and it has a table, reset the table status to AVAILABLE
					if (targetStatusId == cancelledOrderStatusId && order.Table != null)
					{
						order.Table.TableStatusLvId = availableTableStatusId;
						order.Table.UpdatedAt = DateTime.UtcNow;
					}
				}
			}

			if (orderStatusChanged)
			{
				order.UpdatedAt = DateTime.UtcNow;
				await _context.SaveChangesAsync(cancellationToken);
			}
		}
	}


public async Task<long> CreateOrderAsync(Order order, List<OrderItem> items, CancellationToken cancellationToken = default)
{
	_context.Orders.Add(order);
	await _context.SaveChangesAsync(cancellationToken);

	foreach (var item in items)
	{
		item.OrderId = order.OrderId;
	}

	_context.OrderItems.AddRange(items);
	await _context.SaveChangesAsync(cancellationToken);

	return order.OrderId;
}

	public async Task<CustomerOrderHistoryDTO> GetCustomerOrderByIdAsync(long orderId, CancellationToken cancellationToken = default)
	{
		var order = await _context.Orders
			.Where(o => o.OrderId == orderId)
			.Include(o => o.OrderStatusLv)
			.Include(o => o.Table)
			.Include(o => o.OrderItems)
				.ThenInclude(oi => oi.Dish)
			.Include(o => o.OrderItems)
				.ThenInclude(oi => oi.ItemStatusLv)
			.FirstOrDefaultAsync(cancellationToken)
				?? throw new KeyNotFoundException($"Order {orderId} not found.");

		var allItems = order.OrderItems.Select(oi => new OrderItemDTO
		{
			OrderItemId  = oi.OrderItemId,
			DishId       = oi.DishId,
			DishName     = oi.Dish.DishName,
			Quantity     = oi.Quantity,
			Price        = oi.Price,
			ItemStatus   = oi.ItemStatusLv.ValueCode,
			RejectReason = oi.RejectReason,
			Note         = oi.Note
		}).ToList();

		// Exclude rejected and cancelled items from total calculations
		var activeItems = allItems.Where(i => i.ItemStatus != "REJECTED" && i.ItemStatus != "CANCELLED").ToList();
		var totalItems     = activeItems.Sum(i => i.Quantity);
		var estimatedTotal = activeItems.Sum(i => i.Price * i.Quantity);

		return new CustomerOrderHistoryDTO
		{
			TableCode      = order.Table.TableCode,
			TotalItems     = totalItems,
			EstimatedTotal = estimatedTotal,
			Items          = allItems
		};
	}

	public async Task AddItemsToOrderAsync(long orderId, List<OrderItem> items, uint completedOrderStatusId, uint cancelledOrderStatusId, uint pendingOrderStatusId, CancellationToken cancellationToken = default)
	{
		var order = await _context.Orders
			.FirstOrDefaultAsync(o => o.OrderId == orderId, cancellationToken)
				?? throw new KeyNotFoundException($"Order {orderId} not found.");

		foreach (var item in items)
			item.OrderId = orderId;

		_context.OrderItems.AddRange(items);

		// Update total amount on the parent order
		order.TotalAmount += items.Sum(i => i.Price * i.Quantity);
		order.UpdatedAt = DateTime.UtcNow;

		// If the order was completed/cancelled, reopen it to PENDING so kitchen sees the new items
		if (order.OrderStatusLvId == completedOrderStatusId || order.OrderStatusLvId == cancelledOrderStatusId)
			order.OrderStatusLvId = pendingOrderStatusId;

		await _context.SaveChangesAsync(cancellationToken);
	}

	public async Task<CustomerOrderHistoryDTO> GetCustomerOrderHistoryAsync(string tableCode, CancellationToken cancellationToken = default)
	{
		// Include all orders created within the last 24 h to cover late-night sessions
		var since = DateTime.UtcNow.AddHours(-24);

		var orders = await _context.Orders
			.Where(o => o.Table.TableCode == tableCode && o.CreatedAt >= since)
			.Include(o => o.OrderStatusLv)
			.Include(o => o.Table)
			.Include(o => o.OrderItems)
				.ThenInclude(oi => oi.Dish)
			.Include(o => o.OrderItems)
				.ThenInclude(oi => oi.ItemStatusLv)
			.OrderByDescending(o => o.CreatedAt)
			.ToListAsync(cancellationToken);

		// Flatten all items from all orders (newest first)
		var allItems = orders
			.SelectMany(o => o.OrderItems.Select(oi => new OrderItemDTO
			{
				OrderItemId = oi.OrderItemId,
				DishId      = oi.DishId,
				DishName    = oi.Dish.DishName,
				Quantity    = oi.Quantity,
				Price       = oi.Price,
				ItemStatus  = oi.ItemStatusLv.ValueCode,
				RejectReason = oi.RejectReason,
				Note        = oi.Note
			}))
			.ToList();

		// Exclude rejected and cancelled items from total calculations
		var activeItems = allItems.Where(i => i.ItemStatus != "REJECTED" && i.ItemStatus != "CANCELLED").ToList();
		var totalItems     = activeItems.Sum(i => i.Quantity);
		var estimatedTotal = activeItems.Sum(i => i.Price * i.Quantity);

		return new CustomerOrderHistoryDTO
		{
			TableCode      = tableCode,
			TotalItems     = totalItems,
			EstimatedTotal = estimatedTotal,
			Items          = allItems
		};
	}

    public async Task AddAsync(Order order, CancellationToken ct)
    {
        await _context.Orders.AddAsync(order, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<OrderHistoryDTO> GetOrderByIdAsync(
    long orderId,
    CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders
            .AsNoTracking()
            .Where(o => o.OrderId == orderId)
            .Select(o => new OrderHistoryDTO
            {
                OrderId = o.OrderId,
                TableId = o.TableId,
                TableCode = o.Table != null ? o.Table.TableCode : "",

                StaffId = o.StaffId,
                StaffName = o.Staff.FullName,

                CustomerId = o.CustomerId,
                CustomerName = o.Customer != null ? o.Customer.FullName : null,

                TotalAmount = o.TotalAmount,
                TipAmount = o.TipAmount,

                OrderStatus = o.OrderStatusLv.ValueName,
                Source = o.SourceLv.ValueName,

                CreatedAt = o.CreatedAt,
                UpdatedAt = o.UpdatedAt,

                IsPaid = o.Payments.Any(),

                OrderItems = o.OrderItems
                    .Select(oi => new OrderItemDTO
                    {
                        OrderItemId = oi.OrderItemId,
                        DishId = oi.DishId,
                        DishName = oi.Dish.DishName,
                        Quantity = oi.Quantity,
                        Price = oi.Price,
                        ItemStatus = oi.ItemStatusLv.ValueName,
                        RejectReason = oi.RejectReason,
                        Note = oi.Note
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (order == null)
            throw new NotFoundException($"Order with id {orderId} was not found.");

        return order;
    }

    public async Task<Order?> GetByIdForUpdateAsync(long orderId, CancellationToken ct)
    {
        return await _context.Orders
        .Include(x => x.OrderStatusLv)
        .Include(x => x.Payments)
        .Include(x => x.OrderItems)
        .FirstOrDefaultAsync(x => x.OrderId == orderId, ct);
    }

    public async Task<Order?> GetOrderWithItemsAsync(
        long orderId,
        CancellationToken ct)
    {
        return await _context.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(i => i.Dish)
            .FirstOrDefaultAsync(o => o.OrderId == orderId, ct);
    }

    public async Task<Order?> GetActiveOrderByTableAsync(
        long tableId,
        CancellationToken ct)
    {
        return await _context.Orders
            .Where(o => o.TableId == tableId &&
                        o.OrderStatusLv.ValueCode != OrderStatusCode.COMPLETED.ToString() &&
                        o.OrderStatusLv.ValueCode != OrderStatusCode.CANCELLED.ToString())
            .FirstOrDefaultAsync(ct);
    }

    public async Task<List<RecentOrderDTO>> GetRecentOrdersAsync(
    int limit,
    CancellationToken ct)
    {
        return await _context.Orders
            .OrderByDescending(o => o.CreatedAt)
            .Take(limit)
            .Select(o => new RecentOrderDTO
            {
                OrderId = o.OrderId,
                CustomerName = o.Customer.FullName ?? o.Customer.Phone,
                Source = o.SourceLv.ValueCode,
                TableCode = o.Table != null ? o.Table.TableCode : null,
                CreatedAt = o.CreatedAt.Value,
                Status = o.OrderStatusLv.ValueCode
            })
            .ToListAsync(ct);
    }
}
