using Core.DTO.General;
using Core.DTO.Order;
using Core.Entity;
using Core.Enum;
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

	// Guest customer account ID (not a lookup value)
	private const long GuestCustomerId = 68;

	public OrderService(
		IOrderRepository orderRepository,
		ITableRepository tableRepository,
		ICustomerService customerService,
		ILookupResolver lookupResolver)
	{
		_orderRepository = orderRepository;
		_tableRepository = tableRepository;
		_customerService = customerService;
		_lookupResolver = lookupResolver;
	}

	public Task<PagedResultDTO<OrderHistoryDTO>> GetOrderHistoryAsync(OrderHistoryQueryDTO query, CancellationToken cancellationToken = default)
		=> _orderRepository.GetOrderHistoryAsync(query, cancellationToken);

	public Task<OrderStatusCountDTO> GetOrderStatusCountAsync(CancellationToken cancellationToken = default)
		=> _orderRepository.GetOrderStatusCountAsync(cancellationToken);

	public Task<List<KitchenOrderDTO>> GetKitchenOrdersAsync(CancellationToken cancellationToken = default)
		=> _orderRepository.GetKitchenOrdersAsync(cancellationToken);

	public Task UpdateOrderItemStatusAsync(long orderItemId, uint newStatusLvId, string? rejectReason, CancellationToken cancellationToken = default)
		=> _orderRepository.UpdateOrderItemStatusAsync(orderItemId, newStatusLvId, rejectReason, cancellationToken);

	public Task<CustomerOrderHistoryDTO> GetCustomerOrderHistoryAsync(string tableCode, CancellationToken cancellationToken = default)
		=> _orderRepository.GetCustomerOrderHistoryAsync(tableCode, cancellationToken);

	public Task<CustomerOrderHistoryDTO> GetCustomerOrderByIdAsync(long orderId, CancellationToken cancellationToken = default)
		=> _orderRepository.GetCustomerOrderByIdAsync(orderId, cancellationToken);

	public async Task AddItemsToOrderAsync(long orderId, AddOrderItemsRequestDTO request, CancellationToken cancellationToken = default)
	{
		var createdItemLvId = await OrderItemStatusCode.CREATED.ToOrderItemStatusIdAsync(_lookupResolver, cancellationToken);

		var orderItems = request.Items.Select(i => new OrderItem
		{
			DishId         = i.DishId,
			Quantity       = i.Quantity,
			Price          = i.Price,
			Note           = i.Note,
			ItemStatus     = 1,
			ItemStatusLvId = createdItemLvId,
		}).ToList();

		await _orderRepository.AddItemsToOrderAsync(orderId, orderItems, cancellationToken);
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
			TableId          = table.TableId,
			CustomerId       = customerId,
			StaffId          = null,
			TotalAmount      = totalAmount,
			SourceLvId       = dineInSourceLvId,
			OrderStatusLvId  = pendingOrderLvId,
			CreatedAt        = DateTime.UtcNow,
			UpdatedAt        = DateTime.UtcNow,
		};

		// 6. Build OrderItem entities
		var orderItems = request.Items.Select(i => new OrderItem
		{
			DishId          = i.DishId,
			Quantity        = i.Quantity,
			Price           = i.Price,
			Note            = i.Note,
			ItemStatus      = 1, // CREATED
			ItemStatusLvId  = createdItemLvId,
		}).ToList();

		// 7. Save to DB
		var orderId = await _orderRepository.CreateOrderAsync(order, orderItems, cancellationToken);

		// 8. Mark the table as OCCUPIED so other customers cannot select it
		var occupiedLvId = await TableStatusCode.OCCUPIED.ToTableStatusIdAsync(_lookupResolver, cancellationToken);
		await _tableRepository.UpdateStatusAsync(table.TableId, occupiedLvId, cancellationToken);

		return new CreateOrderResponseDTO
		{
			OrderId      = orderId,
			TableId      = table.TableId,
			TableCode    = table.TableCode,
			CustomerId   = customerId,
			TotalAmount  = totalAmount,
			OrderStatus  = "PENDING",
			CreatedAt    = order.CreatedAt,
		};
	}
}
