using Core.DTO.General;
using Core.DTO.Order;
using Core.Entity;
using Core.Enum;
using Core.Exceptions;
using Core.Extensions;
using Core.Interface.Repo;
using Core.Interface.Service.Customer;
using Core.Interface.Service.Entity;
using LookupTypeEnum = Core.Enum.LookupType;

namespace Core.Service;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly ITableRepository _tableRepository;
    private readonly ICustomerService _customerService;
    private readonly ILookupResolver _lookupResolver;
    private readonly IDishRepository _dishRepository;
    private readonly IUnitOfWork _uow;

    public OrderService(
        IOrderRepository orderRepository,
        ITableRepository tableRepository,
        ICustomerService customerService,
        ILookupResolver lookupResolver,
        IDishRepository dishRepository,
        IUnitOfWork uow)
    {
        _orderRepository = orderRepository;
        _tableRepository = tableRepository;
        _customerService = customerService;
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

		return await _orderRepository.GetKitchenOrdersAsync(
			pendingId,
			inProgressId,
			cancellationToken);
	}

	public async Task UpdateOrderItemStatusAsync(long orderItemId, uint newStatusLvId, string? rejectReason, CancellationToken cancellationToken = default)
	{
		var inProgressItemId = await OrderItemStatusCode.IN_PROGRESS.ToOrderItemStatusIdAsync(_lookupResolver, cancellationToken);
		var servedItemId     = await OrderItemStatusCode.SERVED.ToOrderItemStatusIdAsync(_lookupResolver, cancellationToken);
		var rejectedItemId   = await OrderItemStatusCode.REJECTED.ToOrderItemStatusIdAsync(_lookupResolver, cancellationToken);

		var pendingOrderId    = await OrderStatusCode.PENDING.ToOrderStatusIdAsync(_lookupResolver, cancellationToken);
		var inProgressOrderId = await OrderStatusCode.IN_PROGRESS.ToOrderStatusIdAsync(_lookupResolver, cancellationToken);
		var completedOrderId  = await OrderStatusCode.COMPLETED.ToOrderStatusIdAsync(_lookupResolver, cancellationToken);
		var cancelledOrderId  = await OrderStatusCode.CANCELLED.ToOrderStatusIdAsync(_lookupResolver, cancellationToken);

		await _orderRepository.UpdateOrderItemStatusAsync(
			orderItemId,
			newStatusLvId,
			rejectReason,
			inProgressItemId,
			servedItemId,
			rejectedItemId,
			pendingOrderId,
			inProgressOrderId,
			completedOrderId,
			cancelledOrderId,
			cancellationToken);
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

        // 2. Resolve customer_id
        long customerId;
        if (request.IsGuest)
        {
            customerId = GuestCustomerId;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(request.CustomerPhone))
                throw new ArgumentException("CustomerPhone is required when IsGuest = false.");

            customerId = await _customerService.FindOrCreateCustomerIdAsync(
                request.CustomerPhone.Trim(),
                request.CustomerFullName,
                request.CustomerEmail,
                cancellationToken);
        }

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
}

