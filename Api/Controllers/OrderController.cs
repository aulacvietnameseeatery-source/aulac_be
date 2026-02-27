using Core.Attribute;
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

        [HttpPost]
        public async Task<IActionResult> CreateOrder(
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
        [ProducesResponseType(typeof(ApiResponse<OrderHistoryDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetOrderById(
            long id,
            CancellationToken cancellationToken = default)
        {
            var result = await _orderService.GetOrderByIdAsync(id, cancellationToken);

            return Ok(new ApiResponse<OrderHistoryDTO>
            {
                Success = true,
                Code = StatusCodes.Status200OK,
                SubCode = 0,
                UserMessage = "Get order detail successfully",
                Data = result,
                ServerTime = DateTimeOffset.UtcNow
            });
        }

        [HttpPost("{id:long}/items")]
        //[HasPermission(Permissions.UpdateOrderItemStatus)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> AddItems(
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
    }
}
