using Core.DTO.General;
using Core.DTO.Order;
using Core.Entity;
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
				(o.Staff != null && o.Staff.FullName != null && o.Staff.FullName.ToLower().Contains(searchTerm)) ||
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

	public async Task<List<KitchenOrderDTO>> GetKitchenOrdersAsync(CancellationToken cancellationToken = default)
	{
		const uint pendingId    = 28;
		const uint inProgressId = 29;

		var orders = await _context.Orders
			.Include(o => o.OrderStatusLv)
			.Include(o => o.Table)
			.Include(o => o.OrderItems)
				.ThenInclude(oi => oi.Dish)
			.Include(o => o.OrderItems)
				.ThenInclude(oi => oi.ItemStatusLv)
			.Where(o => o.OrderStatusLvId == pendingId || o.OrderStatusLvId == inProgressId)
			.OrderBy(o => o.CreatedAt)
			.Select(o => new KitchenOrderDTO
			{
				OrderId = o.OrderId,
				TableCode = o.Table.TableCode,
				OrderStatus = o.OrderStatusLv.ValueName,
				CreatedAt = o.CreatedAt,
				Items = o.OrderItems.Select(oi => new KitchenOrderItemDTO
				{
					OrderItemId = oi.OrderItemId,
					DishName = oi.Dish.DishName,
					Quantity = oi.Quantity,
					ItemStatus = oi.ItemStatusLv.ValueName,
					Note = oi.Note,
					RejectReason = oi.RejectReason
				}).ToList()
			})
			.ToListAsync(cancellationToken);

		return orders;
	}

	public async Task UpdateOrderItemStatusAsync(long orderItemId, uint newStatusLvId, string? rejectReason, CancellationToken cancellationToken = default)
	{
		var item = await _context.OrderItems
			.FirstOrDefaultAsync(oi => oi.OrderItemId == orderItemId, cancellationToken)
			?? throw new KeyNotFoundException($"OrderItem {orderItemId} not found");

		item.ItemStatusLvId = newStatusLvId;
		item.RejectReason = rejectReason;

		await _context.SaveChangesAsync(cancellationToken);

		// ─── Auto-update Order Status based on Items ──────────────────────
		const uint inProgressItemLvId = 36;
		const uint servedItemLvId     = 38;
		const uint rejectedItemLvId   = 39;

		const uint pendingOrderLvId    = 28;
		const uint inProgressOrderLvId = 29;
		const uint completedOrderLvId  = 30;
		const uint cancelledOrderLvId  = 31;

		var order = await _context.Orders
			.FirstOrDefaultAsync(o => o.OrderId == item.OrderId, cancellationToken);

		if (order != null)
		{
			bool orderStatusChanged = false;

			// 1. Move from PENDING to IN_PROGRESS if an item starts
			if (newStatusLvId == inProgressItemLvId && order.OrderStatusLvId == pendingOrderLvId)
			{
				order.OrderStatusLvId = inProgressOrderLvId;
				orderStatusChanged = true;
			}

			// 2. Auto-complete or auto-cancel when ALL items are finished
			var allItems = await _context.OrderItems
				.Where(oi => oi.OrderId == item.OrderId)
				.Select(oi => oi.ItemStatusLvId)
				.ToListAsync(cancellationToken);

			bool allFinished = allItems.All(lvId => lvId == servedItemLvId || lvId == rejectedItemLvId);

			if (allFinished)
			{
				bool hasServed = allItems.Any(lvId => lvId == servedItemLvId);
				uint targetStatusId = hasServed ? completedOrderLvId : cancelledOrderLvId;

				if (order.OrderStatusLvId != targetStatusId)
				{
					order.OrderStatusLvId = targetStatusId;
					orderStatusChanged = true;
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

		var round = new CustomerOrderRoundDTO
		{
			OrderId     = order.OrderId,
			RoundNumber = 1,
			CreatedAt   = order.CreatedAt,
			OrderStatus = order.OrderStatusLv.ValueName,
			TotalAmount = order.TotalAmount,
			Items = order.OrderItems.Select(oi => new OrderItemDTO
			{
				OrderItemId  = oi.OrderItemId,
				DishId       = oi.DishId,
				DishName     = oi.Dish.DishName,
				Quantity     = oi.Quantity,
				Price        = oi.Price,
				ItemStatus   = oi.ItemStatusLv.ValueName,
				RejectReason = oi.RejectReason,
				Note         = oi.Note
			}).ToList()
		};

		var totalItems     = round.Items.Sum(i => i.Quantity);
		var estimatedTotal = round.Items.Sum(i => i.Price * i.Quantity);

		return new CustomerOrderHistoryDTO
		{
			TableCode      = order.Table.TableCode,
			TotalItems     = totalItems,
			EstimatedTotal = estimatedTotal,
			Rounds         = new List<CustomerOrderRoundDTO> { round }
		};
	}

	public async Task AddItemsToOrderAsync(long orderId, List<OrderItem> items, CancellationToken cancellationToken = default)
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
		const uint completedOrderLvId = 30;
		const uint cancelledOrderLvId = 31;
		const uint pendingOrderLvId   = 28;
		if (order.OrderStatusLvId == completedOrderLvId || order.OrderStatusLvId == cancelledOrderLvId)
			order.OrderStatusLvId = pendingOrderLvId;

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
			.OrderBy(o => o.CreatedAt)
			.ToListAsync(cancellationToken);

		// Assign round numbers (oldest = round 1), then reverse for display (newest first)
		var rounds = orders
			.Select((o, index) => new CustomerOrderRoundDTO
			{
				OrderId = o.OrderId,
				RoundNumber = index + 1,
				CreatedAt = o.CreatedAt,
				OrderStatus = o.OrderStatusLv.ValueName,
				TotalAmount = o.TotalAmount,
				Items = o.OrderItems.Select(oi => new OrderItemDTO
				{
					OrderItemId = oi.OrderItemId,
					DishId      = oi.DishId,
					DishName    = oi.Dish.DishName,
					Quantity    = oi.Quantity,
					Price       = oi.Price,
					ItemStatus  = oi.ItemStatusLv.ValueName,
					RejectReason = oi.RejectReason,
					Note        = oi.Note
				}).ToList()
			})
			.OrderByDescending(r => r.RoundNumber) // newest round first
			.ToList();

		var totalItems     = rounds.SelectMany(r => r.Items).Sum(i => i.Quantity);
		var estimatedTotal = rounds.SelectMany(r => r.Items).Sum(i => i.Price * i.Quantity);

		return new CustomerOrderHistoryDTO
		{
			TableCode      = tableCode,
			TotalItems     = totalItems,
			EstimatedTotal = estimatedTotal,
			Rounds         = rounds
		};
	}
}
