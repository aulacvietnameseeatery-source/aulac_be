using API.Attributes;
using Core.Data;
using Core.DTO.General;
using Core.DTO.Order;
using Core.Enum;
using Core.Extensions;
using Core.Interface.Service.Entity;
using API.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Api.Controllers
{
	[Route("api/orders")]
	[ApiController]
	public class OrderController : ControllerBase
	{
		private readonly IOrderService _orderService;
		private readonly ILookupResolver _lookupResolver;
		private readonly ILogger<OrderController> _logger;

		public OrderController(
			IOrderService orderService,
			ILookupResolver lookupResolver,
			ILogger<OrderController> logger)
		{
			_orderService = orderService;
			_lookupResolver = lookupResolver;
			_logger = logger;
		}

		/// <summary>
		/// Gets paginated order history with optional filters.
		/// </summary>
		/// <param name="query">Pagination and filter parameters</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns>Paginated list of orders</returns>
		/// <response code="200">Orders retrieved successfully</response>
		[HttpGet("history")]
		[HasPermission(Permissions.ViewOrder)]
		[ProducesResponseType(typeof(ApiResponse<PagedResultDTO<OrderHistoryDTO>>), StatusCodes.Status200OK)]
		public async Task<IActionResult> GetOrderHistory(
			[FromQuery] OrderHistoryQueryDTO query,
			CancellationToken cancellationToken = default)
		{
			var result = await _orderService.GetOrderHistoryAsync(query, cancellationToken);

			return Ok(new ApiResponse<PagedResultDTO<OrderHistoryDTO>>
			{
				Success = true,
				Code = 200,
				SubCode = 0,
				UserMessage = "Get order history successfully",
				Data = result,
				ServerTime = DateTimeOffset.Now
			});
		}
		/// <summary>
		/// Gets order counts grouped by status (All, Pending, InProgress, Completed, Cancelled).
		/// Used to display stable badge counts on the tabs regardless of current filter.
		/// </summary>
		[HttpGet("count")]
		[HasPermission(Permissions.ViewOrder)]
		[ProducesResponseType(typeof(ApiResponse<OrderStatusCountDTO>), StatusCodes.Status200OK)]
		public async Task<IActionResult> GetOrderStatusCount(CancellationToken cancellationToken = default)
		{
			var result = await _orderService.GetOrderStatusCountAsync(cancellationToken);

			return Ok(new ApiResponse<OrderStatusCountDTO>
			{
				Success = true,
				Code = 200,
				SubCode = 0,
				UserMessage = "Get order status count successfully",
				Data = result,
				ServerTime = DateTimeOffset.Now
			});
		}

		/// <summary>
		/// Gets all active orders for the kitchen display (Pending + In Progress).
		/// </summary>
		[HttpGet("kitchen")]
		[HasPermission(Permissions.ViewOrder)]
		[ProducesResponseType(typeof(ApiResponse<List<KitchenOrderDTO>>), StatusCodes.Status200OK)]
		public async Task<IActionResult> GetKitchenOrders(CancellationToken cancellationToken = default)
		{
			var result = await _orderService.GetKitchenOrdersAsync(cancellationToken);

			return Ok(new ApiResponse<List<KitchenOrderDTO>>
			{
				Success = true,
				Code = 200,
				SubCode = 0,
				UserMessage = "Get kitchen orders successfully",
				Data = result,
				ServerTime = DateTimeOffset.Now
			});
		}

		/// <summary>
		/// Updates order status for staff operations from order list.
		/// </summary>
		[HttpPatch("{id:long}/status")]
		[HasPermission(Permissions.EditOrder)]
		[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
		public async Task<IActionResult> UpdateOrderStatus(
			long id,
			[FromBody] UpdateOrderStatusDTO dto,
			CancellationToken cancellationToken = default)
		{
			if (!System.Enum.TryParse<OrderStatusCode>(dto.Status, true, out var statusCode))
			{
				return BadRequest(new ApiResponse<object>
				{
					Success = false,
					Code = 400,
					SubCode = 1,
					UserMessage = $"Invalid status: {dto.Status}. Valid values: PENDING, IN_PROGRESS, COMPLETED, CANCELLED",
					ServerTime = DateTimeOffset.Now
				});
			}

			try
			{
				await _orderService.UpdateOrderStatusAsync(id, statusCode, cancellationToken);

				return Ok(new ApiResponse<object>
				{
					Success = true,
					Code = 200,
					SubCode = 0,
					UserMessage = "Order status updated successfully",
					ServerTime = DateTimeOffset.Now
				});
			}
			catch (KeyNotFoundException ex)
			{
				return NotFound(new ApiResponse<object>
				{
					Success = false,
					Code = 404,
					SubCode = 1,
					UserMessage = ex.Message,
					ServerTime = DateTimeOffset.Now
				});
			}
			catch (InvalidOperationException ex)
			{
				return BadRequest(new ApiResponse<object>
				{
					Success = false,
					Code = 400,
					SubCode = 2,
					UserMessage = ex.Message,
					ServerTime = DateTimeOffset.Now
				});
			}
		}

		/// <summary>
		/// Updates the status of a specific order item.
		/// </summary>
		/// <param name="id">Order item ID</param>
		/// <param name="dto">New status and optional reject reason</param>
		/// <param name="cancellationToken">Cancellation token</param>
		[HttpPatch("items/{id}/status")]
		[HasPermission(Permissions.UpdateOrderItemStatus)]
		[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
		public async Task<IActionResult> UpdateOrderItemStatus(
			long id,
			[FromBody] UpdateOrderItemStatusDTO dto,
			CancellationToken cancellationToken = default)
		{
			// Parse and validate the status code
			if (!System.Enum.TryParse<OrderItemStatusCode>(dto.Status, true, out var statusCode))
			{
				return BadRequest(new ApiResponse<object>
				{
					Success = false,
					Code = 400,
					SubCode = 1,
					UserMessage = $"Invalid status: {dto.Status}. Valid values: IN_PROGRESS, READY, SERVED, REJECTED",
					ServerTime = DateTimeOffset.Now
				});
			}

			// REJECTED requires a reason
			if (statusCode == OrderItemStatusCode.REJECTED && string.IsNullOrWhiteSpace(dto.RejectReason))
			{
				return BadRequest(new ApiResponse<object>
				{
					Success = false,
					Code = 400,
					SubCode = 2,
					UserMessage = "RejectReason is required when status is REJECTED",
					ServerTime = DateTimeOffset.Now
				});
			}

			// Resolve enum to lookup_value ID
			var newStatusLvId = await statusCode.ToOrderItemStatusIdAsync(_lookupResolver, cancellationToken);

			await _orderService.UpdateOrderItemStatusAsync(id, newStatusLvId, dto.RejectReason, cancellationToken);

			return Ok(new ApiResponse<object>
			{
				Success = true,
				Code = 200,
				SubCode = 0,
				UserMessage = "Order item status updated successfully",
				ServerTime = DateTimeOffset.Now
			});
		}


	/// <summary>
	/// Returns all orders placed at a given table today, grouped as rounds.
	/// PUBLIC – no authentication required (called from the customer QR menu).
	/// </summary>
	/// <param name="tableCode">The table code from the QR URL, e.g. T1</param>
	[HttpGet("customer/table/{tableCode}")]
	[ProducesResponseType(typeof(ApiResponse<CustomerOrderHistoryDTO>), StatusCodes.Status200OK)]
	public async Task<IActionResult> GetCustomerOrderHistory(
		string tableCode,
		CancellationToken cancellationToken = default)
	{
		var result = await _orderService.GetCustomerOrderHistoryAsync(tableCode, cancellationToken);

		return Ok(new ApiResponse<CustomerOrderHistoryDTO>
		{
			Success = true,
			Code = 200,
			SubCode = 0,
			UserMessage = "Customer order history retrieved successfully",
			Data = result,
			ServerTime = DateTimeOffset.Now
		});
	}

	/// <summary>
	/// Returns all items for a specific order (customer-facing history panel).
	/// PUBLIC – no authentication required.
	/// </summary>
	[HttpGet("{orderId}/customer")]
	[ProducesResponseType(typeof(ApiResponse<CustomerOrderHistoryDTO>), StatusCodes.Status200OK)]
	[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
	public async Task<IActionResult> GetCustomerOrderById(
		long orderId,
		CancellationToken cancellationToken = default)
	{
		try
		{
			var result = await _orderService.GetCustomerOrderByIdAsync(orderId, cancellationToken);

			return Ok(new ApiResponse<CustomerOrderHistoryDTO>
			{
				Success = true,
				Code = 200,
				SubCode = 0,
				UserMessage = "Customer order retrieved successfully",
				Data = result,
				ServerTime = DateTimeOffset.Now
			});
		}
		catch (KeyNotFoundException ex)
		{
			return NotFound(new ApiResponse<object>
			{
				Success = false,
				Code = 404,
				SubCode = 1,
				UserMessage = ex.Message,
				ServerTime = DateTimeOffset.Now
			});
		}
	}

	/// <summary>
	/// Adds more items to an existing order (same table / same customer session).
	/// Called when the customer already has an active order and wants to order additional dishes.
	/// </summary>
	/// <param name="orderId">ID of the existing order to append items to</param>
	/// <param name="dto">List of items to add</param>
	[HttpPost("{orderId}/items")]
	[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
	[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
	public async Task<IActionResult> AddItemsToOrder(
		long orderId,
		[FromBody] AddOrderItemsRequestDTO dto,
		CancellationToken cancellationToken = default)
	{
		try
		{
			await _orderService.AddItemsToOrderAsync(orderId, dto, cancellationToken);

			return Ok(new ApiResponse<object>
			{
				Success = true,
				Code = 200,
				SubCode = 0,
				UserMessage = "Items added to order successfully",
				ServerTime = DateTimeOffset.Now
			});
		}
		catch (KeyNotFoundException ex)
		{
			return NotFound(new ApiResponse<object>
			{
				Success = false,
				Code = 404,
				SubCode = 1,
				UserMessage = ex.Message,
				ServerTime = DateTimeOffset.Now
			});
		}
	}

	/// <summary>
	/// Cancels an order item if it hasn't been started yet (CREATED status only).
	/// PUBLIC – no authentication required (called from customer order history).
	/// </summary>
	/// <param name="orderItemId">ID of the order item to cancel</param>
	[HttpPatch("items/{orderItemId}/cancel")]
	[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
	[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
	[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
	public async Task<IActionResult> CancelOrderItem(
		long orderItemId,
		CancellationToken cancellationToken = default)
	{
		try
		{
			await _orderService.CancelOrderItemAsync(orderItemId, cancellationToken);

			return Ok(new ApiResponse<object>
			{
				Success = true,
				Code = 200,
				SubCode = 0,
				UserMessage = "Order item cancelled successfully",
				ServerTime = DateTimeOffset.Now
			});
		}
		catch (KeyNotFoundException ex)
		{
			return NotFound(new ApiResponse<object>
			{
				Success = false,
				Code = 404,
				SubCode = 1,
				UserMessage = ex.Message,
				ServerTime = DateTimeOffset.Now
			});
		}
		catch (InvalidOperationException ex)
		{
			return BadRequest(new ApiResponse<object>
			{
				Success = false,
				Code = 400,
				SubCode = 1,
				UserMessage = ex.Message,
				ServerTime = DateTimeOffset.Now
			});
		}
	}

	/// <summary>
	/// Creates a new order from the customer-facing menu (QR / DINE_IN flow).
	/// The customer either provides their info or skips (guest account).
	/// </summary>
	/// <param name="dto">Order creation request from the cart</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Created order details</returns>
	[HttpPost]
	[ProducesResponseType(typeof(ApiResponse<CreateOrderResponseDTO>), StatusCodes.Status201Created)]
	[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
	[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
	public async Task<IActionResult> CreateOrder(
		[FromBody] CreateOrderRequestDTO dto,
		CancellationToken cancellationToken = default)
	{
		try
		{
			var result = await _orderService.CreateOrderAsync(dto, cancellationToken);

			return StatusCode(StatusCodes.Status201Created, new ApiResponse<CreateOrderResponseDTO>
			{
				Success = true,
				Code = 201,
				SubCode = 0,
				UserMessage = "Order created successfully",
				Data = result,
				ServerTime = DateTimeOffset.Now
			});
		}
		catch (KeyNotFoundException ex)
		{
			return NotFound(new ApiResponse<object>
			{
				Success = false,
				Code = 404,
				SubCode = 1,
				UserMessage = ex.Message,
				ServerTime = DateTimeOffset.Now
			});
		}
		catch (ArgumentException ex)
		{
			return BadRequest(new ApiResponse<object>
			{
				Success = false,
				Code = 400,
				SubCode = 1,
				UserMessage = ex.Message,
				ServerTime = DateTimeOffset.Now
			});
		}
	}

        /// <summary>
        /// Creates a new order by staff member (for dine-in or takeaway orders entered by restaurant staff).
        /// Requires authentication. Automatically captures the staff member ID from the authenticated user.
        /// </summary>
        /// <param name="request">Order creation request including table, customer, source, and items</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Success response with order creation confirmation</returns>
        /// <response code="200">Order created successfully by staff</response>
        [HttpPost("staff")]
        [HasPermission(Permissions.CreateOrder)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateStaffOrder(
			CreateOrderRequest request,
			CancellationToken ct)
        {
            var staffId = long.Parse(User.FindFirstValue("user_id")!);

            var result = await _orderService.CreateOrderAsync(
            staffId,
            request,
            ct);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Code = 200,
                SubCode = 0,
                UserMessage = "Order created successfully",
                ServerTime = DateTimeOffset.Now
            });
        }

        /// <summary>
        /// Gets order detail by id.
        /// </summary>
        /// <param name="id">Order id</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Order detail</returns>
        /// <response code="200">Order retrieved successfully</response>
        /// <response code="404">Order not found</response>
        [HttpGet("{id:long}")]
        //[HasPermission(Permissions.ViewOrder)]
        [ProducesResponseType(typeof(ApiResponse<OrderDetailDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetOrderById(
            long id,
            CancellationToken cancellationToken = default)
        {
            var result = await _orderService.GetOrderByIdAsync(id, cancellationToken);

            return Ok(new ApiResponse<OrderDetailDTO>
            {
                Success = true,
                Code = StatusCodes.Status200OK,
                SubCode = 0,
                UserMessage = "Get order detail successfully",
                Data = result,
                ServerTime = DateTimeOffset.UtcNow
            });
        }

        /// <summary>
        /// Adds items to an existing order (staff operation).
        /// Used by restaurant staff to add more dishes to an order after it has been created.
        /// Requires authentication.
        /// </summary>
        /// <param name="id">The ID of the order to add items to</param>
        /// <param name="request">List of items to add</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Success response</returns>
        /// <response code="200">Items added successfully</response>
        [HttpPost("staff/{id:long}/items")]
        [HasPermission(Permissions.EditOrder)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> AddStaffOrderItems(
			long id,
			[FromBody] AddOrderItemsRequest request,
			CancellationToken ct)
        {
            await _orderService.AddItemsAsync(id, request, ct);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Code = 200,
                UserMessage = "Items added successfully",
                ServerTime = DateTimeOffset.UtcNow
            });
        }

        /// <summary>
        /// Gets the most recent orders for the current user, limited by the specified count.
        /// Requires authentication and appropriate permissions.
        /// </summary>
        /// <param name="limit">Maximum number of recent orders to return (default: 20)</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>List of recent orders for the user</returns>
        [HttpGet("recent")]
        [HasPermission(Permissions.ViewOrder)]
        public async Task<IActionResult> GetRecentOrders(
        [FromQuery] int limit = 20,
        CancellationToken ct = default)
        {
            var userId = long.Parse(User.FindFirst("user_id")!.Value);

            var roles = User.FindAll(ClaimTypes.Role)
                .Select(r => r.Value)
                .ToList();

            var result = await _orderService.GetRecentOrdersAsync(userId, roles, limit, ct);

            return Ok(new ApiResponse<List<RecentOrderDTO>>
            {
                Success = true,
                Data = result
            });
        }
    }

}

