using Core.Data;
using Core.DTO.General;
using Core.DTO.Notification;
using Core.DTO.Order;
using Core.DTO.Shift;
using Core.Entity;
using Core.Enum;
using Core.Exceptions;
using Core.Extensions;
using Core.Interface.Repo;
using Core.Interface.Service.Customer;
using Core.Interface.Service.Entity;
using Core.Interface.Service.Notification;
using Core.Interface.Service.Others;
using Core.Interface.Service.Shift;
using LookupTypeEnum = Core.Enum.LookupType;

namespace Core.Service;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly ITableRepository _tableRepository;
    private readonly ILookupResolver _lookupResolver;
    private readonly IDishRepository _dishRepository;
    private readonly ICustomerService _customerService;
    private readonly IUnitOfWork _uow;
    private readonly INotificationService _notificationService;
    private readonly IOrderRealtimeService _realtime;
    private readonly IShiftLiveRealtimePublisher _shiftLiveRealtimePublisher;
    private readonly ITaxRepository _taxRepository;

    public OrderService(
        IOrderRepository orderRepository,
        ITableRepository tableRepository,
        ILookupResolver lookupResolver,
        IDishRepository dishRepository,
        ICustomerService customerService,
        IUnitOfWork uow,
        INotificationService notificationService,
        IOrderRealtimeService realtime,
        IShiftLiveRealtimePublisher shiftLiveRealtimePublisher,
        ITaxRepository taxRepository)
    {
        _orderRepository = orderRepository;
        _tableRepository = tableRepository;
        _dishRepository = dishRepository;
        _lookupResolver = lookupResolver;
        _customerService = customerService;
        _uow = uow;
        _notificationService = notificationService;
        _realtime = realtime;
        _shiftLiveRealtimePublisher = shiftLiveRealtimePublisher;
        _taxRepository = taxRepository;
    }

    public Task<PagedResultDTO<OrderHistoryDTO>> GetOrderHistoryAsync(OrderHistoryQueryDTO query, CancellationToken cancellationToken = default)
        => _orderRepository.GetOrderHistoryAsync(query, cancellationToken);

    public async Task<OrderStatusCountDTO> GetOrderStatusCountAsync(CancellationToken cancellationToken = default)
    {
        var pendingId = await OrderStatusCode.PENDING.ToOrderStatusIdAsync(_lookupResolver, cancellationToken);
        var inProgressId = await OrderStatusCode.IN_PROGRESS.ToOrderStatusIdAsync(_lookupResolver, cancellationToken);
        var completedId = await OrderStatusCode.COMPLETED.ToOrderStatusIdAsync(_lookupResolver, cancellationToken);
        var cancelledId = await OrderStatusCode.CANCELLED.ToOrderStatusIdAsync(_lookupResolver, cancellationToken);

        return await _orderRepository.GetOrderStatusCountAsync(
            pendingId,
            inProgressId,
            completedId,
            cancelledId,
            cancellationToken);
    }

    public async Task<List<KitchenOrderDTO>> GetKitchenOrdersAsync(CancellationToken cancellationToken = default)
    {
        var pendingId = await OrderStatusCode.PENDING.ToOrderStatusIdAsync(_lookupResolver, cancellationToken);
        var inProgressId = await OrderStatusCode.IN_PROGRESS.ToOrderStatusIdAsync(_lookupResolver, cancellationToken);
        var completedId = await OrderStatusCode.COMPLETED.ToOrderStatusIdAsync(_lookupResolver, cancellationToken);
        var cancelledId = await OrderStatusCode.CANCELLED.ToOrderStatusIdAsync(_lookupResolver, cancellationToken);

        return await _orderRepository.GetKitchenOrdersAsync(
            pendingId,
            inProgressId,
            completedId,
            cancelledId,
            cancellationToken);
    }

    private const string StaffCancelledReason = "Staff cancelled order";

    public async Task UpdateOrderStatusAsync(long orderId, OrderStatusCode newStatus, CancellationToken cancellationToken = default)
    {
        var pendingStatusId    = await OrderStatusCode.PENDING.ToOrderStatusIdAsync(_lookupResolver, cancellationToken);
        var inProgressStatusId = await OrderStatusCode.IN_PROGRESS.ToOrderStatusIdAsync(_lookupResolver, cancellationToken);
        var completedStatusId  = await OrderStatusCode.COMPLETED.ToOrderStatusIdAsync(_lookupResolver, cancellationToken);
        var cancelledStatusId  = await OrderStatusCode.CANCELLED.ToOrderStatusIdAsync(_lookupResolver, cancellationToken);

        var createdItemStatusId    = await OrderItemStatusCode.CREATED.ToOrderItemStatusIdAsync(_lookupResolver, cancellationToken);
        var inProgressItemStatusId = await OrderItemStatusCode.IN_PROGRESS.ToOrderItemStatusIdAsync(_lookupResolver, cancellationToken);
        var servedItemStatusId     = await OrderItemStatusCode.SERVED.ToOrderItemStatusIdAsync(_lookupResolver, cancellationToken);
        var readyItemStatusId      = await OrderItemStatusCode.READY.ToOrderItemStatusIdAsync(_lookupResolver, cancellationToken);
        var rejectedItemStatusId   = await OrderItemStatusCode.REJECTED.ToOrderItemStatusIdAsync(_lookupResolver, cancellationToken);
        var cancelledItemStatusId  = await OrderItemStatusCode.CANCELLED.ToOrderItemStatusIdAsync(_lookupResolver, cancellationToken);

        var availableTableStatusId = await TableStatusCode.AVAILABLE.ToTableStatusIdAsync(_lookupResolver, cancellationToken);
        var occupiedTableStatusId  = await TableStatusCode.OCCUPIED.ToTableStatusIdAsync(_lookupResolver, cancellationToken);

        var order = await _orderRepository.GetByIdForUpdateAsync(orderId, cancellationToken)
            ?? throw new NotFoundException("Order not found.");

        var current = order.OrderStatusLvId switch
        {
            var id when id == pendingStatusId    => OrderStatusCode.PENDING,
            var id when id == inProgressStatusId => OrderStatusCode.IN_PROGRESS,
            var id when id == completedStatusId  => OrderStatusCode.COMPLETED,
            var id when id == cancelledStatusId  => OrderStatusCode.CANCELLED,
            _ => throw new InvalidOperationException("Current order status is invalid.")
        };

        if (current == newStatus)
            throw new InvalidOperationException("Order is already in this status.");

        var isTransitionAllowed = (current, newStatus) switch
        {
            (OrderStatusCode.PENDING,    OrderStatusCode.IN_PROGRESS) => true,
            (OrderStatusCode.PENDING,    OrderStatusCode.CANCELLED)   => true,
            (OrderStatusCode.IN_PROGRESS, OrderStatusCode.COMPLETED)  => true,
            (OrderStatusCode.IN_PROGRESS, OrderStatusCode.CANCELLED)  => true,
            (OrderStatusCode.CANCELLED,  OrderStatusCode.PENDING)     => true,
            (OrderStatusCode.COMPLETED,  OrderStatusCode.IN_PROGRESS) => true,
            _ => false
        };

        if (!isTransitionAllowed)
            throw new InvalidOperationException($"Invalid status transition: {current} -> {newStatus}.");

        if (newStatus == OrderStatusCode.CANCELLED && order.Payments.Any())
            throw new InvalidOperationException("Cannot cancel a paid order.");

        // ── Reset validation: allow if there are cascade-cancelled OR kitchen-rejected items to restore ──
        if (current == OrderStatusCode.CANCELLED && newStatus == OrderStatusCode.PENDING)
        {
            var hasRestorableItems = order.OrderItems
                .Any(i => (i.RejectReason == StaffCancelledReason && i.ItemStatusLvId == cancelledItemStatusId)
                       || i.ItemStatusLvId == rejectedItemStatusId);

            if (!hasRestorableItems)
                throw new InvalidOperationException("Cannot reset order: no items to restore. Please create a new order.");
        }

        await _uow.BeginTransactionAsync(cancellationToken);
        try
        {
            var targetStatusId = newStatus switch
            {
                OrderStatusCode.PENDING     => pendingStatusId,
                OrderStatusCode.IN_PROGRESS => inProgressStatusId,
                OrderStatusCode.COMPLETED   => completedStatusId,
                OrderStatusCode.CANCELLED   => cancelledStatusId,
                _ => throw new InvalidOperationException("Unsupported order status.")
            };

            order.OrderStatusLvId = targetStatusId;
            order.UpdatedAt = DateTime.UtcNow;

            // ── Cascade Cancel: set active items to CANCELLED with reason ──
            if (newStatus == OrderStatusCode.CANCELLED)
            {
                var terminalStatuses = new HashSet<uint> { servedItemStatusId, readyItemStatusId, rejectedItemStatusId, cancelledItemStatusId };
                foreach (var item in order.OrderItems)
                {
                    if (terminalStatuses.Contains(item.ItemStatusLvId)) continue;

                    item.ItemStatusLvId = cancelledItemStatusId;
                    item.RejectReason   = StaffCancelledReason;
                    order.TotalAmount    -= item.Price * item.Quantity;
                    order.SubTotalAmount -= item.Price * item.Quantity;
                }
            }

            // ── Reset: restore cascade-cancelled items AND kitchen-rejected items ──
            if (current == OrderStatusCode.CANCELLED && newStatus == OrderStatusCode.PENDING)
            {
                foreach (var item in order.OrderItems)
                {
                    var isCascadeCancelled = item.ItemStatusLvId == cancelledItemStatusId && item.RejectReason == StaffCancelledReason;
                    var isKitchenRejected  = item.ItemStatusLvId == rejectedItemStatusId;
                    if (isCascadeCancelled || isKitchenRejected)
                    {
                        item.ItemStatusLvId = createdItemStatusId;
                        item.RejectReason   = null;
                        order.TotalAmount    += item.Price * item.Quantity;
                        order.SubTotalAmount += item.Price * item.Quantity;
                    }
                }
            }

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

            if (newStatus == OrderStatusCode.CANCELLED)
            {
                await _notificationService.PublishAsync(new PublishNotificationRequest
                {
                    Type = nameof(NotificationType.ORDER_CANCELLED),
                    Title = "Order Cancelled",
                    Body = $"Order #{orderId} has been cancelled",
                    Priority = nameof(NotificationPriority.High),
                    SoundKey = "notification_high",
                    ActionUrl = "/dashboard/orders",
                    EntityType = "Order",
                    EntityId = orderId.ToString(),
                    Metadata = new Dictionary<string, object>
                    {
                        ["orderId"] = orderId.ToString()
                    },
                    TargetPermissions = new List<string> { Permissions.UpdateOrderItemStatus }
                }, cancellationToken);
            }

            await _realtime.OrderUpdatedAsync(new OrderRealtimeDTO
            {
                OrderId = orderId,
                Status = newStatus.ToString(),
                TableId = order.TableId,
                UpdatedAt = DateTime.UtcNow
            });

            await PublishShiftLiveOrderEventAsync("order_status_changed", orderId, order.StaffId, cancellationToken);
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
        var readyItemId = await OrderItemStatusCode.READY.ToOrderItemStatusIdAsync(_lookupResolver, cancellationToken);
        var servedItemId = await OrderItemStatusCode.SERVED.ToOrderItemStatusIdAsync(_lookupResolver, cancellationToken);
        var rejectedItemId = await OrderItemStatusCode.REJECTED.ToOrderItemStatusIdAsync(_lookupResolver, cancellationToken);
        var cancelledItemId = await OrderItemStatusCode.CANCELLED.ToOrderItemStatusIdAsync(_lookupResolver, cancellationToken);

        var pendingOrderId = await OrderStatusCode.PENDING.ToOrderStatusIdAsync(_lookupResolver, cancellationToken);
        var inProgressOrderId = await OrderStatusCode.IN_PROGRESS.ToOrderStatusIdAsync(_lookupResolver, cancellationToken);
        var completedOrderId = await OrderStatusCode.COMPLETED.ToOrderStatusIdAsync(_lookupResolver, cancellationToken);
        var cancelledOrderId = await OrderStatusCode.CANCELLED.ToOrderStatusIdAsync(_lookupResolver, cancellationToken);
        var availableTableId = await TableStatusCode.AVAILABLE.ToTableStatusIdAsync(_lookupResolver, cancellationToken);

        await _orderRepository.UpdateOrderItemStatusAsync(
            orderItemId,
            newStatusLvId,
            rejectReason,
            inProgressItemId,
            readyItemId,
            servedItemId,
            rejectedItemId,
            cancelledItemId,
            pendingOrderId,
            inProgressOrderId,
            completedOrderId,
            cancelledOrderId,
            availableTableId,
            cancellationToken);

        // Fire notification for READY / REJECTED item status changes
        if (newStatusLvId == readyItemId)
        {
            await _notificationService.PublishAsync(new PublishNotificationRequest
            {
                Type = nameof(NotificationType.ORDER_ITEM_READY),
                Title = "Order Item Ready",
                Body = $"An item in order is ready to serve",
                Priority = nameof(NotificationPriority.High),
                SoundKey = "notification_high",
                ActionUrl = "/dashboard/orders",
                EntityType = "OrderItem",
                EntityId = orderItemId.ToString(),
                Metadata = new Dictionary<string, object>
                {
                    ["orderItemId"] = orderItemId.ToString()
                },
                TargetPermissions = new List<string> { Permissions.ViewOrder }
            }, cancellationToken);
        }
        else if (newStatusLvId == rejectedItemId)
        {
            await _notificationService.PublishAsync(new PublishNotificationRequest
            {
                Type = nameof(NotificationType.ORDER_ITEM_REJECTED),
                Title = "Order Item Rejected",
                Body = $"An item in order was rejected" + (rejectReason != null ? $": {rejectReason}" : ""),
                Priority = nameof(NotificationPriority.High),
                SoundKey = "notification_high",
                ActionUrl = "/dashboard/orders",
                EntityType = "OrderItem",
                EntityId = orderItemId.ToString(),
                Metadata = new Dictionary<string, object>
                {
                    ["orderItemId"] = orderItemId.ToString(),
                    ["reason"] = rejectReason ?? ""
                },
                TargetPermissions = new List<string> { Permissions.ViewOrder }
            }, cancellationToken);
        }

        var updatedOrderItem = await _orderRepository.GetOrderItemAsync(orderItemId, cancellationToken);

        await _shiftLiveRealtimePublisher.PublishBoardChangedAsync(new ShiftLiveRealtimeEventDto
        {
            EventType = "order_item_changed",
            WorkDate = DateOnly.FromDateTime(DateTime.UtcNow),
            OccurredAt = DateTime.UtcNow,
        }, cancellationToken);
    }

    public async Task CancelOrderItemAsync(long orderItemId, CancellationToken cancellationToken = default)
    {
        // Fetch item details for rich notification before cancelling
        var orderItem = await _orderRepository.GetOrderItemAsync(orderItemId, cancellationToken);
        var dishName = orderItem?.Dish?.DishName ?? "";
        var tableCode = orderItem?.Order?.Table?.TableCode ?? "";

        var cancelledStatusId = await OrderItemStatusCode.CANCELLED.ToOrderItemStatusIdAsync(_lookupResolver, cancellationToken);

        // Update to CANCELLED status (repository will validate if item is CREATED)
        await UpdateOrderItemStatusAsync(orderItemId, cancelledStatusId, null, cancellationToken);

        await _notificationService.PublishAsync(new PublishNotificationRequest
        {
            Type = nameof(NotificationType.ORDER_ITEM_CANCELLED),
            Priority = nameof(NotificationPriority.High),
            SoundKey = "notification_high",
            ActionUrl = "/dashboard/kitchen",
            EntityType = "OrderItem",
            EntityId = orderItemId.ToString(),
            Metadata = new Dictionary<string, object>
            {
                ["orderItemId"] = orderItemId.ToString(),
                ["tableCode"] = tableCode,
                ["dishName"] = dishName
            },
            TargetPermissions = new List<string> { Permissions.UpdateOrderItemStatus }
        }, cancellationToken);
    }

    public Task<OrderDetailDTO> GetOrderByIdAsync(long orderId, CancellationToken cancellationToken = default)
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

            var customerId = await _customerService.ResolveCustomerAsync(request.Customer, ct);

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
            order.SubTotalAmount = total;

            await ApplyTaxToOrderAsync(order, ct);

            await _orderRepository.AddAsync(order, ct);



            await _uow.CommitAsync(ct);

            await _realtime.OrderCreatedAsync(new OrderRealtimeDTO
            {
                OrderId = order.OrderId,
                Status = OrderStatusCode.PENDING.ToString(),
                TableId = order.TableId,
                UpdatedAt = DateTime.UtcNow
            });

            await PublishShiftLiveOrderEventAsync("order_created", order.OrderId, order.StaffId, ct);

            // Notify kitchen staff about new order
            await _notificationService.PublishAsync(new PublishNotificationRequest
            {
                Type = nameof(NotificationType.NEW_ORDER),
                Title = "New Order",
                Body = $"Order #{order.OrderId} with {request.Items.Count} item(s)",
                Priority = nameof(NotificationPriority.High),
                SoundKey = "notification_high",
                ActionUrl = "/dashboard/kitchen",
                EntityType = "Order",
                EntityId = order.OrderId.ToString(),
                Metadata = new Dictionary<string, object>
                {
                    ["orderId"] = order.OrderId.ToString(),
                    ["itemCount"] = request.Items.Count.ToString(),
                    ["tableCode"] = table?.TableCode ?? "Takeaway",
                    ["totalAmount"] = total.ToString("F2")
                },
                TargetPermissions = new List<string> { Permissions.UpdateOrderItemStatus }
            }, ct);

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

            // ===== Load order =====

            var order = await _orderRepository.GetByIdForUpdateAsync(orderId, ct)
                ?? throw new NotFoundException("Order not found.");

            // ===== Business Validation =====

            if (order.OrderStatusLvId == canceledStatusId)
                throw new InvalidOperationException("Cannot add items to canceled order.");

            if (order.Payments.Any())
                throw new InvalidOperationException("Cannot add items to paid order.");

            // ===== CUSTOMER RESOLUTION =====

            if (request.Customer != null && !string.IsNullOrWhiteSpace(request.Customer.Phone))
            {
                var customerId = await _customerService.ResolveCustomerAsync(
                    request.Customer,
                    ct);

                // Attach customer if different
                if (order.CustomerId != customerId)
                {
                    order.CustomerId = customerId;
                }
            }

            var newStatusCode = order.OrderStatusLv.ValueCode;

            if (request.Items.Any())
            {
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
                order.SubTotalAmount += additionalTotal;
                order.UpdatedAt = DateTime.UtcNow;

                await ApplyTaxToOrderAsync(order, ct);

                // ===== Status transition logic =====

                if (order.OrderStatusLvId == completedStatusId)
                {
                    order.OrderStatusLvId = inProgressStatusId;
                    newStatusCode = OrderStatusCode.IN_PROGRESS.ToString();
                }
            }

            await _uow.SaveChangesAsync(ct);

            await _uow.CommitAsync(ct);

            await _realtime.OrderUpdatedAsync(new OrderRealtimeDTO
            {
                OrderId = orderId,
                Status = newStatusCode,
                TableId = order.TableId,
                UpdatedAt = DateTime.UtcNow
            });

            await PublishShiftLiveOrderEventAsync("order_items_added", orderId, order.StaffId, ct);
        }
        catch
        {
            await _uow.RollbackAsync(ct);
            throw;
        }
    }


    public Task<CustomerOrderHistoryDTO> GetCustomerOrderByIdAsync(long orderId, CancellationToken cancellationToken = default)
        => _orderRepository.GetCustomerOrderByIdAsync(orderId, cancellationToken);

    public async Task AddItemsToOrderAsync(long orderId, AddOrderItemsRequestDTO request, CancellationToken cancellationToken = default)
    {
        // Pre-check: block if order already has a payment
        var existingOrder = await _orderRepository.GetByIdForUpdateAsync(orderId, cancellationToken)
            ?? throw new KeyNotFoundException($"Order {orderId} not found.");

        if (existingOrder.Payments.Any())
            throw new InvalidOperationException("This order has already been paid. Please ask staff to create a new order for your table.");

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


        // Fetch dish names and table info for rich notification
        var dishIds = request.Items.Select(i => i.DishId).Distinct().ToList();
        var dishes = await _dishRepository.GetByIdsAsync(dishIds, cancellationToken);
        var dishNames = string.Join(", ", request.Items
            .Select(i => dishes.FirstOrDefault(d => d.DishId == i.DishId)?.DishName)
            .Where(n => n != null));

        var tableCode = "";
        if (existingOrder?.TableId != null)
        {
            var table = await _tableRepository.GetByIdAsync(existingOrder.TableId.Value, cancellationToken);
            tableCode = table?.TableCode ?? "";
        }

        await _notificationService.PublishAsync(new PublishNotificationRequest
        {
            Type = nameof(NotificationType.ORDER_ITEMS_ADDED),
            Priority = nameof(NotificationPriority.High),
            SoundKey = "notification_high",
            ActionUrl = "/dashboard/kitchen",
            EntityType = "Order",
            EntityId = orderId.ToString(),
            Metadata = new Dictionary<string, object>
            {
                ["orderId"] = orderId.ToString(),
                ["tableCode"] = tableCode,
                ["itemCount"] = request.Items.Count.ToString(),
                ["dishNames"] = dishNames
            },
            TargetPermissions = new List<string> { Permissions.UpdateOrderItemStatus }
        }, cancellationToken);

        // Recalculate tax after adding items
        var order = await _orderRepository.GetByIdForUpdateAsync(orderId, cancellationToken);
        if (order != null)
        {
            await ApplyTaxToOrderAsync(order, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);
        }

    }


    public async Task<CreateOrderResponseDTO> CreateOrderAsync(CreateOrderRequestDTO request, CancellationToken cancellationToken = default)
    {
        // Start transaction to prevent race conditions
        await _uow.BeginTransactionAsync(cancellationToken);

        try
        {
            // 1. Resolve table_id from table code
            var table = await _tableRepository.GetByCodeAsync(request.TableCode.Trim(), cancellationToken)
                ?? throw new KeyNotFoundException($"Table '{request.TableCode}' not found.");

            // 2. Validate QR token if provided (QR scan flow)
            if (!string.IsNullOrEmpty(request.QrToken) && !string.Equals(table.QrToken, request.QrToken, StringComparison.Ordinal))
                throw new ValidationException("Invalid QR token. Please scan the QR code on the table again.");

            // 3. Verify table status and lock the table atomically
            var availableLvId = await TableStatusCode.AVAILABLE.ToTableStatusIdAsync(_lookupResolver, cancellationToken);
            var occupiedLvId = await TableStatusCode.OCCUPIED.ToTableStatusIdAsync(_lookupResolver, cancellationToken);
            var lockedLvId = await TableStatusCode.LOCKED.ToTableStatusIdAsync(_lookupResolver, cancellationToken);
            var reservedLvId = await TableStatusCode.RESERVED.ToTableStatusIdAsync(_lookupResolver, cancellationToken);

            // Block if table is LOCKED (maintenance) or deleted
            if (table.TableStatusLvId == lockedLvId)
                throw new InvalidOperationException($"Table '{request.TableCode}' is under maintenance and cannot be used.");

            if (table.IsDeleted)
                throw new InvalidOperationException($"Table '{request.TableCode}' is no longer available.");

            // If table is OCCUPIED, find the existing active order and add items to it
            if (table.TableStatusLvId == occupiedLvId)
            {
                var existingOrder = await _orderRepository.GetActiveOrderByTableAsync(table.TableId, cancellationToken);
                if (existingOrder != null)
                {
                    var createdItemLvIdForExisting = await OrderItemStatusCode.CREATED.ToOrderItemStatusIdAsync(_lookupResolver, cancellationToken);
                    var completedOrderLvId = await OrderStatusCode.COMPLETED.ToOrderStatusIdAsync(_lookupResolver, cancellationToken);
                    var cancelledOrderLvId = await OrderStatusCode.CANCELLED.ToOrderStatusIdAsync(_lookupResolver, cancellationToken);
                    var pendingOrderLvId2 = await OrderStatusCode.PENDING.ToOrderStatusIdAsync(_lookupResolver, cancellationToken);

                    var additionalItems = request.Items.Select(i => new OrderItem
                    {
                        DishId = i.DishId,
                        Quantity = i.Quantity,
                        Price = i.Price,
                        Note = i.Note,
                        ItemStatus = 1,
                        ItemStatusLvId = createdItemLvIdForExisting,
                    }).ToList();

                    await _orderRepository.AddItemsToOrderAsync(existingOrder.OrderId, additionalItems, completedOrderLvId, cancelledOrderLvId, pendingOrderLvId2, cancellationToken);
                    await ApplyTaxToOrderAsync(existingOrder, cancellationToken);
                    await _uow.CommitAsync(cancellationToken);

                    await _realtime.OrderCreatedAsync(new OrderRealtimeDTO
                    {
                        OrderId = existingOrder.OrderId,
                        Status = OrderStatusCode.PENDING.ToString(),
                        TableId = table.TableId,
                        UpdatedAt = DateTime.UtcNow
                    });

                    await PublishShiftLiveOrderEventAsync("customer_order_created", existingOrder.OrderId, null, cancellationToken);

                    await _notificationService.PublishAsync(new PublishNotificationRequest
                    {
                        Type = nameof(NotificationType.NEW_ORDER),
                        Priority = nameof(NotificationPriority.High),
                        SoundKey = "notification_high",
                        ActionUrl = "/dashboard/kitchen",
                        EntityType = "Order",
                        EntityId = existingOrder.OrderId.ToString(),
                        Metadata = new Dictionary<string, object>
                        {
                            ["orderId"] = existingOrder.OrderId.ToString(),
                            ["itemCount"] = request.Items.Count.ToString(),
                            ["tableCode"] = table.TableCode
                        },
                        TargetPermissions = new List<string> { Permissions.UpdateOrderItemStatus }
                    }, cancellationToken);

                    return new CreateOrderResponseDTO
                    {
                        OrderId = existingOrder.OrderId,
                        TableId = table.TableId,
                        TableCode = table.TableCode,
                        CustomerId = existingOrder.CustomerId,
                        TotalAmount = existingOrder.TotalAmount,
                        OrderStatus = "PENDING",
                        CreatedAt = existingOrder.CreatedAt,
                    };
                }
                // If no active orders but status is OCCUPIED (edge case), fall through to create new order
            }

            // Change status to OCCUPIED if currently AVAILABLE or RESERVED
            if (table.TableStatusLvId == availableLvId || table.TableStatusLvId == reservedLvId)
            {
                table.TableStatusLvId = occupiedLvId;
                table.UpdatedAt = DateTime.UtcNow;
                await _tableRepository.UpdateAsync(table, cancellationToken);
            }

            // 4. Always use guest account for customer-facing orders
            var customerId = await _customerService.GetGuestCustomerIdAsync(cancellationToken);

            // 5. Calculate total amount from items
            var totalAmount = request.Items.Sum(i => i.Price * i.Quantity);

            // 6. Resolve lookup value IDs from database
            var pendingOrderLvId = await OrderStatusCode.PENDING.ToOrderStatusIdAsync(_lookupResolver, cancellationToken);
            var dineInSourceLvId = await OrderSourceCode.DINE_IN.IdAsync(_lookupResolver, (ushort)LookupTypeEnum.OrderSource, cancellationToken);
            var createdItemLvId = await OrderItemStatusCode.CREATED.ToOrderItemStatusIdAsync(_lookupResolver, cancellationToken);

            // 7. Build Order entity (staff_id = null, status = PENDING, source = DINE_IN)
            var order = new Order
            {
                TableId = table.TableId,
                CustomerId = customerId,
                StaffId = null,
                TotalAmount = totalAmount,
                SubTotalAmount = totalAmount,
                SourceLvId = dineInSourceLvId,
                OrderStatusLvId = pendingOrderLvId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            await ApplyTaxToOrderAsync(order, cancellationToken);


            // 8. Build OrderItem entities
            var orderItems = request.Items.Select(i => new OrderItem
            {
                DishId = i.DishId,
                Quantity = i.Quantity,
                Price = i.Price,
                Note = i.Note,
                ItemStatus = 1, // CREATED
                ItemStatusLvId = createdItemLvId,
            }).ToList();

            // 9. Save to DB
            var orderId = await _orderRepository.CreateOrderAsync(order, orderItems, cancellationToken);

            // Commit transaction
            await _uow.CommitAsync(cancellationToken);

            await _realtime.OrderCreatedAsync(new OrderRealtimeDTO
            {
                OrderId = orderId,
                Status = OrderStatusCode.PENDING.ToString(),
                TableId = table.TableId,
                UpdatedAt = DateTime.UtcNow
            });

            await PublishShiftLiveOrderEventAsync("customer_order_created", orderId, null, cancellationToken);

            await _notificationService.PublishAsync(new PublishNotificationRequest
            {
                Type = nameof(NotificationType.NEW_ORDER),
                Priority = nameof(NotificationPriority.High),
                SoundKey = "notification_high",
                ActionUrl = "/dashboard/kitchen",
                EntityType = "Order",
                EntityId = orderId.ToString(),
                Metadata = new Dictionary<string, object>
                {
                    ["orderId"] = orderId.ToString(),
                    ["itemCount"] = request.Items.Count.ToString(),
                    ["tableCode"] = table.TableCode
                },
                TargetPermissions = new List<string> { Permissions.UpdateOrderItemStatus }
            }, cancellationToken);

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
        catch
        {
            await _uow.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<List<RecentOrderDTO>> GetRecentOrdersAsync(
        long userId,
        List<string> roles,
        int limit,
        CancellationToken ct)
    {
        if (limit <= 0 || limit > 100)
            limit = 20;

        return await _orderRepository.GetRecentOrdersAsync(userId, roles, limit, ct);
    }

    // ── Lookup ID bag — resolved once and shared across all UpdateOrderItems helpers ──────────
    private sealed record UpdateOrderIds(
        uint CancelledOrderId, uint PendingOrderId, uint CompletedOrderId, uint InProgressOrderId,
        uint CreatedItemId, uint InProgressItemId, uint ServedItemId,
        uint RejectedItemId, uint CancelledItemId, uint ReadyItemId,
        uint AvailableTableId, uint OccupiedTableId);

    private async Task<UpdateOrderIds> ResolveUpdateOrderIdsAsync(CancellationToken ct) => new(
        CancelledOrderId:  await OrderStatusCode.CANCELLED.ToOrderStatusIdAsync(_lookupResolver, ct),
        PendingOrderId:    await OrderStatusCode.PENDING.ToOrderStatusIdAsync(_lookupResolver, ct),
        CompletedOrderId:  await OrderStatusCode.COMPLETED.ToOrderStatusIdAsync(_lookupResolver, ct),
        InProgressOrderId: await OrderStatusCode.IN_PROGRESS.ToOrderStatusIdAsync(_lookupResolver, ct),
        CreatedItemId:     await OrderItemStatusCode.CREATED.ToOrderItemStatusIdAsync(_lookupResolver, ct),
        InProgressItemId:  await OrderItemStatusCode.IN_PROGRESS.ToOrderItemStatusIdAsync(_lookupResolver, ct),
        ServedItemId:      await OrderItemStatusCode.SERVED.ToOrderItemStatusIdAsync(_lookupResolver, ct),
        RejectedItemId:    await OrderItemStatusCode.REJECTED.ToOrderItemStatusIdAsync(_lookupResolver, ct),
        CancelledItemId:   await OrderItemStatusCode.CANCELLED.ToOrderItemStatusIdAsync(_lookupResolver, ct),
        ReadyItemId:       await OrderItemStatusCode.READY.ToOrderItemStatusIdAsync(_lookupResolver, ct),
        AvailableTableId:  await TableStatusCode.AVAILABLE.ToTableStatusIdAsync(_lookupResolver, ct),
        OccupiedTableId:   await TableStatusCode.OCCUPIED.ToTableStatusIdAsync(_lookupResolver, ct));

    // ─────────────────────────────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task UpdateOrderItemsAsync(long orderId, UpdateOrderItemsRequest request, CancellationToken ct)
    {
        var ids = await ResolveUpdateOrderIdsAsync(ct);

        await _uow.BeginTransactionAsync(ct);
        try
        {
            // ── 1. Load & validate order ──
            var order = await _orderRepository.GetByIdForUpdateAsync(orderId, ct)
                ?? throw new NotFoundException($"Order {orderId} not found.");

            if (order.OrderStatusLvId == ids.CancelledOrderId)
                throw new InvalidOperationException("Cannot modify a cancelled order.");

            if (order.Payments.Any())
                throw new InvalidOperationException("Cannot modify a paid order.");

            // ── 2. Apply item adjustments ──
            foreach (var adj in request.Adjustments)
                ApplySingleAdjustment(order, adj, ids);

            // ── 3. Append new items ──
            var newOrderStatus = order.OrderStatusLv.ValueCode;
            if (request.NewItems.Any())
                newOrderStatus = await AppendNewItemsAsync(order, request.NewItems, ids, ct) ?? newOrderStatus;

            // ── 4. Auto-recalculate order status when removals were processed ──
            if (request.Adjustments.Any(a => a.NewQuantity <= 0) && !request.NewItems.Any())
                newOrderStatus = await RecalculateOrderStatusAfterRemovalAsync(order, ids, ct) ?? newOrderStatus;

            // ── 5. Update customer ──
            if (request.Customer != null && !string.IsNullOrWhiteSpace(request.Customer.Phone))
            {
                var customerId = await _customerService.ResolveCustomerAsync(request.Customer, ct);
                if (order.CustomerId != customerId)
                    order.CustomerId = customerId;
            }

            // ── 6. Recalculate totals ──
            order.TotalAmount    = order.OrderItems
                .Where(i => i.ItemStatusLvId != ids.RejectedItemId && i.ItemStatusLvId != ids.CancelledItemId)
                .Sum(i => i.Price * i.Quantity);
            order.SubTotalAmount = order.TotalAmount;
            order.UpdatedAt      = DateTime.UtcNow;

            await ApplyTaxToOrderAsync(order, ct);
            await _uow.SaveChangesAsync(ct);
            await _uow.CommitAsync(ct);

            // ── 7. Realtime event ──
            await _realtime.OrderUpdatedAsync(new OrderRealtimeDTO
            {
                OrderId   = orderId,
                Status    = newOrderStatus,
                TableId   = order.TableId,
                UpdatedAt = DateTime.UtcNow,
            });

            await PublishShiftLiveOrderEventAsync("order_updated", orderId, order.StaffId, ct);
        }
        catch
        {
            await _uow.RollbackAsync(ct);
            throw;
        }
    }

    /// <summary>Validates and applies one adjustment to an existing order item.</summary>
    private void ApplySingleAdjustment(Order order, OrderItemAdjustmentDto adj, UpdateOrderIds ids)
    {
        var item = order.OrderItems.FirstOrDefault(i => i.OrderItemId == adj.OrderItemId)
            ?? throw new NotFoundException($"Order item {adj.OrderItemId} not found in order {order.OrderId}.");

        if (item.ItemStatusLvId == ids.InProgressItemId)
            throw new InvalidOperationException($"Item {adj.OrderItemId} is IN_PROGRESS and cannot be modified.");

        if (item.ItemStatusLvId == ids.RejectedItemId)
            throw new InvalidOperationException($"Item {adj.OrderItemId} is REJECTED and cannot be modified.");

        if (item.ItemStatusLvId == ids.ServedItemId)
        {
            if (adj.NewQuantity > item.Quantity)
                throw new InvalidOperationException(
                    $"Item {adj.OrderItemId} has already been SERVED — quantity cannot be increased.");

            if (string.IsNullOrWhiteSpace(adj.Reason))
                throw new InvalidOperationException(
                    $"A reason is required when adjusting a SERVED item (item {adj.OrderItemId}).");
        }

        if (adj.NewQuantity <= 0)
        {
            if (item.ItemStatusLvId == ids.CreatedItemId)
            {
                // CREATED: physically remove from the order
                order.OrderItems.Remove(item);
                _uow.Remove(item);
            }
            else
            {
                // Non-CREATED (e.g. SERVED): mark as REJECTED with audit trail
                item.ItemStatusLvId = ids.RejectedItemId;
                item.ItemStatus     = 5; // REJECTED ordinal
                item.RejectReason   = adj.Reason;
            }
        }
        else if (item.ItemStatusLvId == ids.CreatedItemId)
        {
            // CREATED: simple qty + note edit
            item.Quantity = adj.NewQuantity;
            if (adj.Note != null) item.Note = adj.Note;
        }
        else
        {
            // SERVED: update qty and append structured audit-trail note (parseable by FE for i18n)
            item.Quantity = adj.NewQuantity;
            item.Note     = BuildAdjustedNote(item.Note, adj.NewQuantity, adj.Reason);
        }
    }

    /// <summary>
    /// Builds the structured audit-trail note for a SERVED item adjustment.
    /// Replaces any existing ADJUSTED segment while preserving the original base note.
    /// </summary>
    private static string BuildAdjustedNote(string? existing, int newQty, string? reason)
    {
        var raw    = existing ?? "";
        var adjIdx = raw.IndexOf("ADJUSTED:");
        string? baseNote = adjIdx < 0
            ? (string.IsNullOrWhiteSpace(raw) ? null : raw.Trim())
            : (raw[..adjIdx].TrimEnd().TrimEnd('|').TrimEnd() is { Length: > 0 } b ? b : null);

        var segment = $"ADJUSTED:{newQty}|REASON:{reason}";
        return baseNote is null ? segment : $"{baseNote} | {segment}";
    }

    /// <summary>
    /// Appends new dishes to the order.
    /// Returns the updated order status string if the order status changed (COMPLETED → IN_PROGRESS),
    /// or <c>null</c> if no status change occurred.
    /// </summary>
    private async Task<string?> AppendNewItemsAsync(
        Order order, List<CreateOrderItemDto> newItems, UpdateOrderIds ids, CancellationToken ct)
    {
        var dishIds = newItems.Select(i => i.DishId).Distinct().ToList();
        var dishes  = await _dishRepository.GetByIdsAsync(dishIds, ct);

        if (dishes.Count != dishIds.Count)
            throw new NotFoundException("One or more dishes not found.");

        foreach (var newItem in newItems)
        {
            var dish = dishes.First(d => d.DishId == newItem.DishId);
            order.OrderItems.Add(new OrderItem
            {
                OrderId        = order.OrderId,
                DishId         = newItem.DishId,
                Quantity       = newItem.Quantity,
                Price          = dish.Price,
                Note           = newItem.Note,
                ItemStatus     = 1,            // CREATED ordinal
                ItemStatusLvId = ids.CreatedItemId,
            });
        }

        // COMPLETED → IN_PROGRESS: order is re-opened when new items arrive
        if (order.OrderStatusLvId == ids.CompletedOrderId)
        {
            order.OrderStatusLvId = ids.InProgressOrderId;
            return OrderStatusCode.IN_PROGRESS.ToString();
        }

        return null;
    }

    /// <summary>
    /// Recalculates order status after one or more items were removed (qty → 0).
    /// Cancels or completes the order when all remaining items are in a terminal state.
    /// Also frees the table if the order is cancelled and the table is still OCCUPIED.
    /// Returns the updated order status string, or <c>null</c> if the status did not change.
    /// </summary>
    private async Task<string?> RecalculateOrderStatusAfterRemovalAsync(
        Order order, UpdateOrderIds ids, CancellationToken ct)
    {
        var allStatuses = order.OrderItems.Select(i => i.ItemStatusLvId).ToList();

        if (allStatuses.Count == 0)
        {
            // All items deleted — cancel order and free table
            if (order.OrderStatusLvId != ids.CancelledOrderId)
                order.OrderStatusLvId = ids.CancelledOrderId;

            await FreeTableIfOccupiedAsync(order, ids, ct);
            return OrderStatusCode.CANCELLED.ToString();
        }

        bool allFinished = allStatuses.All(s =>
            s == ids.ServedItemId  || s == ids.ReadyItemId ||
            s == ids.RejectedItemId || s == ids.CancelledItemId);

        if (!allFinished) return null;

        bool hasBeenWorked  = allStatuses.Any(s => s == ids.ServedItemId || s == ids.ReadyItemId);
        var  targetStatusId = hasBeenWorked ? ids.CompletedOrderId : ids.CancelledOrderId;

        if (order.OrderStatusLvId == targetStatusId) return null;

        order.OrderStatusLvId = targetStatusId;

        if (!hasBeenWorked)
            await FreeTableIfOccupiedAsync(order, ids, ct);

        return hasBeenWorked ? OrderStatusCode.COMPLETED.ToString() : OrderStatusCode.CANCELLED.ToString();
    }

    /// <summary>Sets the order's table to AVAILABLE if it is currently OCCUPIED.</summary>
    private async Task FreeTableIfOccupiedAsync(Order order, UpdateOrderIds ids, CancellationToken ct)
    {
        if (!order.TableId.HasValue) return;

        var table = await _tableRepository.GetByIdAsync(order.TableId.Value, ct);
        if (table is null || table.TableStatusLvId != ids.OccupiedTableId) return;

        table.TableStatusLvId = ids.AvailableTableId;
        table.UpdatedAt       = DateTime.UtcNow;
        await _tableRepository.UpdateAsync(table, ct);
    }

    // ─────────────────────────────────────────────────────────────────────────────────────────

    private Task PublishShiftLiveOrderEventAsync(string eventType, long orderId, long? staffId, CancellationToken ct)
    {
        return _shiftLiveRealtimePublisher.PublishBoardChangedAsync(new ShiftLiveRealtimeEventDto
        {
            EventType = eventType,
            WorkDate = DateOnly.FromDateTime(DateTime.UtcNow),
            StaffId = staffId,
            OrderId = orderId,
            OccurredAt = DateTime.UtcNow,
        }, ct);
    }

    private async Task ApplyTaxToOrderAsync(Order order, CancellationToken ct)
    {
        if (order.TaxId.HasValue)
        {
            var tax = await _taxRepository.GetByIdAsync(order.TaxId.Value, ct);
            if (tax != null)
            {
                if (tax.TaxType == "EXCLUSIVE")
                {
                    order.TaxAmount = order.TotalAmount * (tax.TaxRate / 100);
                }
                else // INCLUSIVE
                {
                    order.TaxAmount = order.TotalAmount - (order.TotalAmount / (1 + tax.TaxRate / 100));
                }
            }
        }
        else
        {
            var defaultTax = await _taxRepository.GetDefaultTaxAsync(ct);
            if (defaultTax != null)
            {
                order.TaxId = defaultTax.TaxId;
                if (defaultTax.TaxType == "EXCLUSIVE")
                {
                    order.TaxAmount = order.TotalAmount * (defaultTax.TaxRate / 100);
                }
                else // INCLUSIVE
                {
                    order.TaxAmount = order.TotalAmount - (order.TotalAmount / (1 + defaultTax.TaxRate / 100));
                }
            }
        }
    }
}


