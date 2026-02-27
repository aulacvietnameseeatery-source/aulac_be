using Core.DTO.General;
using Core.DTO.Order;
using Core.Entity;
using Core.Enum;
using Core.Exceptions;
using Core.Interface.Repo;
using Core.Interface.Service.Entity;

namespace Core.Service;

public class OrderService : IOrderService
{
	private readonly IOrderRepository _orderRepository;
    private readonly ITableRepository _tableRepository;
    private readonly IDishRepository _dishRepository;
    private readonly ILookupResolver _lookupResolver;
    private readonly IUnitOfWork _uow;

    private const long GUEST_CUSTOMER_ID = 68; // ID representing a visitor

    public OrderService(IOrderRepository orderRepository,
        ITableRepository tableRepository,
        IDishRepository dishRepository,
        ILookupResolver lookupResolver,
        IUnitOfWork uow)
	{
		_orderRepository = orderRepository;
        _tableRepository = tableRepository;
        _dishRepository = dishRepository;
        _lookupResolver = lookupResolver;
        _uow = uow;
    }

	public Task<PagedResultDTO<OrderHistoryDTO>> GetOrderHistoryAsync(OrderHistoryQueryDTO query, CancellationToken cancellationToken = default)
		=> _orderRepository.GetOrderHistoryAsync(query, cancellationToken);

	public Task<OrderStatusCountDTO> GetOrderStatusCountAsync(CancellationToken cancellationToken = default)
		=> _orderRepository.GetOrderStatusCountAsync(cancellationToken);

	public Task<List<KitchenOrderDTO>> GetKitchenOrdersAsync(CancellationToken cancellationToken = default)
		=> _orderRepository.GetKitchenOrdersAsync(cancellationToken);

	public Task UpdateOrderItemStatusAsync(long orderItemId, uint newStatusLvId, string? rejectReason, CancellationToken cancellationToken = default)
		=> _orderRepository.UpdateOrderItemStatusAsync(orderItemId, newStatusLvId, rejectReason, cancellationToken);

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

            var customerId = request.CustomerId ?? GUEST_CUSTOMER_ID;

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
}

