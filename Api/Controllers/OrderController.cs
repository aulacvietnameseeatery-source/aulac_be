using Core.Attribute;
using Core.Data;
using Core.DTO.General;
using Core.DTO.Order;
using Core.Interface.Service.Entity;
using API.Models;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
	[Route("api/orders")]
	[ApiController]
	public class OrderController : ControllerBase
	{
		private readonly IOrderService _orderService;
		private readonly ILogger<OrderController> _logger;

		public OrderController(
			IOrderService orderService,
			ILogger<OrderController> logger)
		{
			_orderService = orderService;
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
	}
}
