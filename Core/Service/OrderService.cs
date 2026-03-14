using Core.DTO.General;
using Core.DTO.Order;
using Core.Entity;
using Core.Enum;
using Core.Exceptions;
using Core.Extensions;
using Core.Interface.Repo;
using Core.Interface.Service.Entity;
using LookupTypeEnum = Core.Enum.LookupType;

namespace Core.Service;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly ITableRepository _tableRepository;
    private readonly ILookupResolver _lookupResolver;
    private readonly IDishRepository _dishRepository;
    private readonly IUnitOfWork _uow;

    public OrderService(
        IOrderRepository orderRepository,
        ITableRepository tableRepository,
        ILookupResolver lookupResolver,
        IDishRepository dishRepository,
        IUnitOfWork uow)
    {
        _orderRepository = orderRepository;
        _tableRepository = tableRepository;
        _dishRepository = dishRepository;
        _lookupResolver = lookupResolver;
        _uow = uow;
    }
    private const long GuestCustomerId = 68; // ID representing a visitor
    public Task<PagedResultDTO<OrderHistoryDTO>> GetOrderHistoryAsync(OrderHistoryQueryDTO query, CancellationToken cancellationToken = default)
		=> _orderRepository.GetOrderHistoryAsync(query, cancellationToken);

	public async Task<OrderStatusCountDTO> GetOrderStatusCountAsync(CancellationToken cancellationToken = default)
	{
		var pendingId    = await OrderStatusCode.PENDING.ToOrderStatusIdAsync(_lookupResolver, cancellationToken);
		var inProgressId = await OrderStatusCode.IN_PROGRESS.ToOrderStatusIdAsync(_lookupResolver, cancellationToken);
		var completedId  = await OrderStatusCode.COMPLETED.ToOrderStatusIdAsync(_lookupResolver, cancellationToken);
		var cancelledId  = await OrderStatusCode.CANCELLED.ToOrderStatusIdAsync(_lookupResolver, cancellationToken);

		return await _orderRepository.GetOrderStatusCountAsync(
			pendingId,
			inProgressId,
			completedId,
			cancelledId,
			cancellationToken);
	}

	public async Task<List<KitchenOrderDTO>> GetKitchenOrdersAsync(CancellationToken cancellationToken = default)
	{
		var pendingId    = await OrderStatusCode.PENDING.ToOrderStatusIdAsync(_lookupResolver, cancellationToken);
		var inProgressId = await OrderStatusCode.IN_PROGRESS.ToOrderStatusIdAsync(_lookupResolver, cancellationToken);
		var completedId  = await OrderStatusCode.COMPLETED.ToOrderStatusIdAsync(_lookupResolver, cancellationToken);
		var cancelledId  = await OrderStatusCode.CANCELLED.ToOrderStatusIdAsync(_lookupResolver, cancellationToken);

		return await _orderRepository.GetKitchenOrdersAsync(
			pendingId,
			inProgressId,
			completedId,
			cancelledId,
			cancellationToken);
	}

    public async Task UpdateOrderStatusAsync(long orderId, OrderStatusCode newStatus, CancellationToken cancellationToken = default)
    {
        var pendingStatusId = await OrderStatusCode.PENDING.ToOrderStatusIdAsync(_lookupResolver, cancellationToken);
        var inProgressStatusId = await OrderStatusCode.IN_PROGRESS.ToOrderStatusIdAsync(_lookupResolver, cancellationToken);
        var completedStatusId = await OrderStatusCode.COMPLETED.ToOrderStatusIdAsync(_lookupResolver, cancellationToken);
        var cancelledStatusId = await OrderStatusCode.CANCELLED.ToOrderStatusIdAsync(_lookupResolver, cancellationToken);

        var availableTableStatusId = await TableStatusCode.AVAILABLE.ToTableStatusIdAsync(_lookupResolver, cancellationToken);
        var occupiedTableStatusId = await TableStatusCode.OCCUPIED.ToTableStatusIdAsync(_lookupResolver, cancellationToken);

        var order = await _orderRepository.GetByIdForUpdateAsync(orderId, cancellationToken)
            ?? throw new NotFoundException("Order not found.");

        var current = order.OrderStatusLvId switch
        {
            var id when id == pendingStatusId => OrderStatusCode.PENDING,
            var id when id == inProgressStatusId => OrderStatusCode.IN_PROGRESS,
            var id when id == completedStatusId => OrderStatusCode.COMPLETED,
            var id when id == cancelledStatusId => OrderStatusCode.CANCELLED,
            _ => throw new InvalidOperationException("Current order status is invalid.")
        };

        if (current == newStatus)
            throw new InvalidOperationException("Order is already in this status.");

        var isTransitionAllowed = (current, newStatus) switch
        {
            (OrderStatusCode.PENDING, OrderStatusCode.IN_PROGRESS) => true,
            (OrderStatusCode.PENDING, OrderStatusCode.CANCELLED) => true,
            (OrderStatusCode.IN_PROGRESS, OrderStatusCode.COMPLETED) => true,
            (OrderStatusCode.IN_PROGRESS, OrderStatusCode.CANCELLED) => true,
            (OrderStatusCode.CANCELLED, OrderStatusCode.PENDING) => true,
            (OrderStatusCode.COMPLETED, OrderStatusCode.IN_PROGRESS) => true,
            _ => false
        };

        if (!isTransitionAllowed)
            throw new InvalidOperationException($"Invalid status transition: {current} -> {newStatus}.");

        if (newStatus == OrderStatusCode.CANCELLED && order.Payments.Any())
            throw new InvalidOperationException("Cannot cancel a paid order.");

        await _uow.BeginTransactionAsync(cancellationToken);
        try
        {
            var targetStatusId = newStatus switch
            {
                OrderStatusCode.PENDING => pendingStatusId,
                OrderStatusCode.IN_PROGRESS => inProgressStatusId,
                OrderStatusCode.COMPLETED => completedStatusId,
                OrderStatusCode.CANCELLED => cancelledStatusId,
                _ => throw new InvalidOperationException("Unsupported order status.")
            };

            order.OrderStatusLvId = targetStatusId;
            order.UpdatedAt = DateTime.UtcNow;

            if (order.TableId.HasValue)
            {
                var table = await _tableRepository.GetByIdAsync(order.TableId.Value, cancellationToken);
                if (table != null)
                {
                    if (newStatus == OrderStatusCode.CANCELLED || newStatus == OrderStatusCode.COMPLETED)
                    {
                        table.TableStatusLvId = availableTableStatusId;
                        table.UpdatedAt = DateTime.UtcNow;
                        await _tableRepository.UpdateAsync(table, cancellationToken);
                    }
                    else if (current == OrderStatusCode.CANCELLED && newStatus == OrderStatusCode.PENDING)
                    {
                        table.TableStatusLvId = occupiedTableStatusId;
                        table.UpdatedAt = DateTime.UtcNow;
                        await _tableRepository.UpdateAsync(table, cancellationToken);
                    }
                }
            }

            await _uow.SaveChangesAsync(cancellationToken);
            await _uow.CommitAsync(cancellationToken);
        }
        catch
        {
            await _uow.RollbackAsync(cancellationToken);
            throw;
        }
    }

	public async Task UpdateOrderItemStatusAsync(long orderItemId, uint newStatusLvId, string? rejectReason, CancellationToken cancellationToken = default)
	{
		var inProgressItemId = await OrderItemStatusCode.IN_PROGRESS.ToOrderItemStatusIdAsync(_lookupResolver, cancellationToken);
		var readyItemId      = await OrderItemStatusCode.READY.ToOrderItemStatusIdAsync(_lookupResolver, cancellationToken);
		var servedItemId     = await OrderItemStatusCode.SERVED.ToOrderItemStatusIdAsync(_lookupResolver, cancellationToken);
		var rejectedItemId   = await OrderItemStatusCode.REJECTED.ToOrderItemStatusIdAsync(_lookupResolver, cancellationToken);

		var pendingOrderId    = await OrderStatusCode.PENDING.ToOrderStatusIdAsync(_lookupResolver, cancellationToken);
		var inProgressOrderId = await OrderStatusCode.IN_PROGRESS.ToOrderStatusIdAsync(_lookupResolver, cancellationToken);
		var completedOrderId  = await OrderStatusCode.COMPLETED.ToOrderStatusIdAsync(_lookupResolver, cancellationToken);
		var cancelledOrderId  = await OrderStatusCode.CANCELLED.ToOrderStatusIdAsync(_lookupResolver, cancellationToken);
		var availableTableId  = await TableStatusCode.AVAILABLE.ToTableStatusIdAsync(_lookupResolver, cancellationToken);

		await _orderRepository.UpdateOrderItemStatusAsync(
			orderItemId,
			newStatusLvId,
			rejectReason,
			inProgressItemId,
			readyItemId,
			servedItemId,
			rejectedItemId,
			pendingOrderId,
			inProgressOrderId,
			completedOrderId,
			cancelledOrderId,
			availableTableId,
			cancellationToken);
	}

	public async Task CancelOrderItemAsync(long orderItemId, CancellationToken cancellationToken = default)
	{
		// Get CREATED and CANCELLED status IDs
		var createdStatusId = await OrderItemStatusCode.CREATED.ToOrderItemStatusIdAsync(_lookupResolver, cancellationToken);
		var cancelledStatusId = await OrderItemStatusCode.CANCELLED.ToOrderItemStatusIdAsync(_lookupResolver, cancellationToken);

		// Update to CANCELLED status (repository will validate if item is CREATED)
		await UpdateOrderItemStatusAsync(orderItemId, cancelledStatusId, null, cancellationToken);
	}

    public Task<OrderHistoryDTO> GetOrderByIdAsync(long orderId, CancellationToken cancellationToken = default)
        => _orderRepository.GetOrderByIdAsync(orderId, cancellationToken);

    public async Task<long> CreateOrderAsync(long staffId, CreateOrderRequest request, CancellationToken ct)

    {
        // ===== VALIDATION =====

        if (!request.Items.Any())
            throw new InvalidOperationException("Order must contain at least one item.");

        if (request.Source == OrderSourceCode.DINE_IN && request.TableId == null)
            throw new InvalidOperationException("DINE_IN order requires table.");

        if (request.Source == OrderSourceCode.TAKEAWAY && request.TableId != null)
            throw new InvalidOperationException("TAKEAWAY cannot have table.");

        await _uow.BeginTransactionAsync(ct);

        try
        {
            // ===== LOOKUP IDS =====

            var orderStatusId = await _lookupResolver.GetIdAsync(
                (ushort)Enum.LookupType.OrderStatus,
                OrderStatusCode.PENDING,
                ct);

            var orderItemStatusId = await _lookupResolver.GetIdAsync(
                (ushort)Enum.LookupType.OrderItemStatus,
                OrderItemStatusCode.CREATED,
                ct);

            var sourceId = await _lookupResolver.GetIdAsync(
                (ushort)Enum.LookupType.OrderSource,
                request.Source,
                ct);

            var occupiedTableStatusId = await _lookupResolver.GetIdAsync(
            (ushort)Enum.LookupType.TableStatus,
            TableStatusCode.OCCUPIED,
            ct);

            var lockedTableStatusId = await _lookupResolver.GetIdAsync(
                (ushort)Enum.LookupType.TableStatus,
                TableStatusCode.LOCKED,
                ct);

            // ===== HANDLE TABLE =====

            RestaurantTable? table = null;

            if (request.Source == OrderSourceCode.DINE_IN)
            {
                table = await _tableRepository.GetByIdAsync(request.TableId!.Value, ct)
                    ?? throw new NotFoundException("Table not found.");

                if (table.TableStatusLvId == lockedTableStatusId || table.TableStatusLvId == occupiedTableStatusId)
                    throw new InvalidOperationException("Table is not available.");

                table.TableStatusLvId = occupiedTableStatusId;
                await _tableRepository.UpdateAsync(table, ct);
            }

            var customerId = request.CustomerId ?? GuestCustomerId;

            // ===== DISH LOAD (1 QUERY) =====

            var dishIds = request.Items
                .Select(x => x.DishId)
                .Distinct()
                .ToList();

            var dishes = await _dishRepository.GetByIdsAsync(dishIds, ct);

            if (dishes.Count != dishIds.Count)
                throw new NotFoundException("One or more dishes not found.");

            // ===== CREATE ORDER =====

            var order = new Order
            {
                StaffId = staffId,
                CustomerId = customerId,
                TableId = request.TableId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                TipAmount = 0,
                OrderStatusLvId = orderStatusId,
                SourceLvId = sourceId
            };

            decimal total = 0;

            foreach (var item in request.Items)
            {
                var dish = dishes.First(d => d.DishId == item.DishId);

                var price = dish.Price;

                total += price * item.Quantity;

                order.OrderItems.Add(new OrderItem
                {
                    DishId = dish.DishId,
                    Quantity = item.Quantity,
                    Price = price,
                    Note = item.Note,
                    ItemStatusLvId = orderItemStatusId
                });
            }

            order.TotalAmount = total;

            await _orderRepository.AddAsync(order, ct);

            await _uow.CommitAsync(ct);

            return order.OrderId;
        }
        catch
        {
            await _uow.RollbackAsync(ct);
            throw;
        }
    }

    public async Task AddItemsAsync(
    long orderId,
    AddOrderItemsRequest request,
    CancellationToken ct)
    {
        if (!request.Items.Any())
            throw new InvalidOperationException("Must add at least one item.");

        await _uow.BeginTransactionAsync(ct);

        try
        {
            // ===== Lookup needed statuses =====

            var canceledStatusId = await _lookupResolver.GetIdAsync(
                (ushort)Enum.LookupType.OrderStatus,
                OrderStatusCode.CANCELLED,
                ct);

            var completedStatusId = await _lookupResolver.GetIdAsync(
                (ushort)Enum.LookupType.OrderStatus,
                OrderStatusCode.COMPLETED,
                ct);

            var inProgressStatusId = await _lookupResolver.GetIdAsync(
                (ushort)Enum.LookupType.OrderStatus,
                OrderStatusCode.IN_PROGRESS,
                ct);

            var createdItemStatusId = await _lookupResolver.GetIdAsync(
                (ushort)Enum.LookupType.OrderItemStatus,
                OrderItemStatusCode.CREATED,
                ct);

            // ===== Load order WITH LOCK (concurrency safe) =====

            var order = await _orderRepository.GetByIdForUpdateAsync(orderId, ct)
                ?? throw new NotFoundException("Order not found.");

            // ===== Business Validation =====

            if (order.OrderStatusLvId == canceledStatusId)
                throw new InvalidOperationException("Cannot add items to canceled order.");

            if (order.Payments.Any())
                throw new InvalidOperationException("Cannot add items to paid order.");

            // ===== Load dishes in 1 query =====

            var dishIds = request.Items
                .Select(x => x.DishId)
                .Distinct()
                .ToList();

            var dishes = await _dishRepository.GetByIdsAsync(dishIds, ct);

            if (dishes.Count != dishIds.Count)
                throw new NotFoundException("One or more dishes not found.");

            decimal additionalTotal = 0;

            foreach (var item in request.Items)
            {
                var dish = dishes.First(d => d.DishId == item.DishId);

                var price = dish.Price;

                additionalTotal += price * item.Quantity;

                order.OrderItems.Add(new OrderItem
                {
                    DishId = dish.DishId,
                    Quantity = item.Quantity,
                    Price = price,
                    Note = item.Note,
                    ItemStatusLvId = createdItemStatusId
                });
            }

            // ===== Update total =====

            order.TotalAmount += additionalTotal;
            order.UpdatedAt = DateTime.UtcNow;

            // ===== Status transition logic =====

            if (order.OrderStatusLvId == completedStatusId)
            {
                order.OrderStatusLvId = inProgressStatusId;
            }

            await _uow.SaveChangesAsync(ct);

            await _uow.CommitAsync(ct);
        }
        catch
        {
            await _uow.RollbackAsync(ct);
            throw;
        }
    }



    public Task<CustomerOrderHistoryDTO> GetCustomerOrderHistoryAsync(string tableCode, CancellationToken cancellationToken = default)
        => _orderRepository.GetCustomerOrderHistoryAsync(tableCode, cancellationToken);

    public Task<CustomerOrderHistoryDTO> GetCustomerOrderByIdAsync(long orderId, CancellationToken cancellationToken = default)
        => _orderRepository.GetCustomerOrderByIdAsync(orderId, cancellationToken);

    public async Task AddItemsToOrderAsync(long orderId, AddOrderItemsRequestDTO request, CancellationToken cancellationToken = default)
    {
        var createdItemLvId = await OrderItemStatusCode.CREATED.ToOrderItemStatusIdAsync(_lookupResolver, cancellationToken);
        var completedOrderStatusId = await OrderStatusCode.COMPLETED.ToOrderStatusIdAsync(_lookupResolver, cancellationToken);
        var cancelledOrderStatusId = await OrderStatusCode.CANCELLED.ToOrderStatusIdAsync(_lookupResolver, cancellationToken);
        var pendingOrderStatusId = await OrderStatusCode.PENDING.ToOrderStatusIdAsync(_lookupResolver, cancellationToken);

        var orderItems = request.Items.Select(i => new OrderItem
        {
            DishId = i.DishId,
            Quantity = i.Quantity,
            Price = i.Price,
            Note = i.Note,
            ItemStatus = 1,
            ItemStatusLvId = createdItemLvId,
        }).ToList();

        await _orderRepository.AddItemsToOrderAsync(orderId, orderItems, completedOrderStatusId, cancelledOrderStatusId, pendingOrderStatusId, cancellationToken);
    }

    public async Task<CreateOrderResponseDTO> CreateOrderAsync(CreateOrderRequestDTO request, CancellationToken cancellationToken = default)
    {
        // 1. Resolve table_id from table code
        var table = await _tableRepository.GetByCodeAsync(request.TableCode.Trim(), cancellationToken)
            ?? throw new KeyNotFoundException($"Table '{request.TableCode}' not found.");

        // 2. Always use guest account for customer-facing orders
        var customerId = GuestCustomerId;

        // 3. Calculate total amount from items
        var totalAmount = request.Items.Sum(i => i.Price * i.Quantity);

        // 4. Resolve lookup value IDs from database
        var pendingOrderLvId = await OrderStatusCode.PENDING.ToOrderStatusIdAsync(_lookupResolver, cancellationToken);
        var dineInSourceLvId = await OrderSourceCode.DINE_IN.IdAsync(_lookupResolver, (ushort)LookupTypeEnum.OrderSource, cancellationToken);
        var createdItemLvId = await OrderItemStatusCode.CREATED.ToOrderItemStatusIdAsync(_lookupResolver, cancellationToken);

        // 5. Build Order entity (staff_id = null, status = PENDING, source = DINE_IN)
        var order = new Order
        {
            TableId = table.TableId,
            CustomerId = customerId,
            StaffId = null,
            TotalAmount = totalAmount,
            SourceLvId = dineInSourceLvId,
            OrderStatusLvId = pendingOrderLvId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        // 6. Build OrderItem entities
        var orderItems = request.Items.Select(i => new OrderItem
        {
            DishId = i.DishId,
            Quantity = i.Quantity,
            Price = i.Price,
            Note = i.Note,
            ItemStatus = 1, // CREATED
            ItemStatusLvId = createdItemLvId,
        }).ToList();

        // 7. Save to DB
        var orderId = await _orderRepository.CreateOrderAsync(order, orderItems, cancellationToken);

       

        return new CreateOrderResponseDTO
        {
            OrderId = orderId,
            TableId = table.TableId,
            TableCode = table.TableCode,
            CustomerId = customerId,
            TotalAmount = totalAmount,
            OrderStatus = "PENDING",
            CreatedAt = order.CreatedAt,
        };
    }

    public async Task<List<RecentOrderDTO>> GetRecentOrdersAsync(
        int limit,
        CancellationToken ct)
    {
        if (limit <= 0 || limit > 100)
            limit = 20;

        return await _orderRepository.GetRecentOrdersAsync(limit, ct);
    }
}

