using API.Models;
using Core.DTO.Customer;
using Core.DTO.Notification;
using Core.DTO.Reservation;
using Core.Enum;
using Core.Interface.Service.Customer;
using Core.Interface.Service.Entity;
using Core.Interface.Service.Notification;
using Core.Interface.Service.Reservation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>
/// Public API controller for table reservations.
/// All endpoints are anonymous (no authentication required).
/// </summary>
[Route("api/public")]
[ApiController]
[AllowAnonymous]
public class PublicReservationController : ControllerBase
{
    private readonly IPublicReservationService _reservationService;
    private readonly ICustomerService _customerService;
    private readonly ITableService _tableService;
    private readonly INotificationService _notificationService;
    private readonly IOrderService _orderService;
    private readonly ILogger<PublicReservationController> _logger;

    public PublicReservationController(
        IPublicReservationService reservationService,
        ICustomerService customerService,
        ITableService tableService,
        INotificationService notificationService,
        IOrderService orderService,
        ILogger<PublicReservationController> logger)
    {
        _reservationService = reservationService;
        _customerService = customerService;
        _tableService = tableService;
        _notificationService = notificationService;
        _orderService = orderService;
        _logger = logger;
    }

    [HttpGet("customers/phone/{phone}")]
    [ProducesResponseType(typeof(ApiResponse<CustomerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCustomerByPhone(string phone)
    {
        var customer = await _customerService.GetByPhoneAsync(phone);

        if (customer == null)
        {
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Code = 200,
                UserMessage = "Customer not found.",
                Data = new { },
                ServerTime = DateTimeOffset.UtcNow
            });
        }

        return Ok(new ApiResponse<CustomerDto>
        {
            Success = true,
            Code = 200,
            UserMessage = "Customer retrieved successfully.",
            Data = customer,
            ServerTime = DateTimeOffset.UtcNow
        });
    }

    [HttpPost("reservations/fit")]
    [ProducesResponseType(typeof(ApiResponse<ReservationFitCheckResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckReservationFit(
        [FromBody] ReservationFitCheckRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _reservationService.CheckReservationFitAsync(request, cancellationToken);

        return Ok(new ApiResponse<ReservationFitCheckResponse>
        {
            Success = true,
            Code = 200,
            UserMessage = result.CanBookOnline ? "Online reservation is available." : "No suitable online table arrangement found.",
            Data = result,
            ServerTime = DateTimeOffset.UtcNow
        });
    }


    /// <summary>
    /// Submits a final reservation.
    /// </summary>
    /// <param name="request">Reservation details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Confirmed reservation details</returns>
    /// <response code="200">Reservation created successfully</response>
    /// <response code="400">Invalid or expired lock token</response>
    /// <response code="404">Table not found</response>
    [HttpPost("reservations")]
    [ProducesResponseType(typeof(ApiResponse<ReservationResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateReservation(
        [FromBody] CreateReservationRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _reservationService.CreateReservationAsync(request, cancellationToken);

        _logger.LogInformation(
            "Reservation {ReservationId} created for {CustomerName}",
            result.ReservationId, result.CustomerName);

        return Ok(new ApiResponse<ReservationResponseDto>
        {
            Success = true,
            Code = 200,
            UserMessage = "Reservation request created successfully.",
            SystemMessage = "Reservation created in pending status",
            Data = result,
            ServerTime = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Marks a table as occupied when a customer starts dining.
    /// This is a public endpoint used by customers via QR code.
    /// </summary>
    /// <param name="tableCode">The table code (e.g., TB-R01)</param>
    /// <param name="token">Optional QR token for validation (from QR scan URL)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success response</returns>
    /// <response code="200">Table marked as occupied successfully</response>
    /// <response code="404">Table not found</response>
    [HttpPost("tables/{tableCode}/occupy")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> OccupyTable(
        string tableCode,
        [FromQuery] string? token = null,
        CancellationToken cancellationToken = default)
    {
        await _tableService.OccupyTableByCodeAsync(tableCode, token, cancellationToken);

        _logger.LogInformation("Table {TableCode} marked as occupied by customer", tableCode);

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Code = 200,
            UserMessage = "Table marked as occupied successfully.",
            SystemMessage = "Table status updated",
            Data = new { },
            ServerTime = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Allows a customer to request payment for their order.
    /// Sends a PAYMENT_REQUEST notification to staff with ProcessPayment permission.
    /// No authentication required — called directly from the customer-facing menu.
    /// </summary>
    [HttpPost("orders/{orderId:long}/request-payment")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RequestPayment(
        long orderId,
        [FromQuery] string? tableCode = null,
        CancellationToken cancellationToken = default)
    {
        if (orderId <= 0)
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Code = 400,
                UserMessage = "Invalid order ID.",
                ServerTime = DateTimeOffset.UtcNow
            });
        }

        // Check if all non-rejected/non-cancelled items are SERVED
        var orderHistory = await _orderService.GetCustomerOrderByIdAsync(orderId, cancellationToken);
        var unservedItems = orderHistory.Items
            .Where(i => i.ItemStatus != "REJECTED" && i.ItemStatus != "CANCELLED" && i.ItemStatus != "SERVED")
            .ToList();

        if (unservedItems.Any())
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Code = 400,
                SubCode = 10,
                UserMessage = "Bạn vẫn còn món chưa được phục vụ. Vui lòng đợi hoàn tất trước khi thanh toán.",
                ServerTime = DateTimeOffset.UtcNow
            });
        }

        var tableInfo = string.IsNullOrWhiteSpace(tableCode) ? $"Order #{orderId}" : $"Table {tableCode}";

        await _notificationService.PublishAsync(new PublishNotificationRequest
        {
            Type = nameof(NotificationType.PAYMENT_REQUEST),
            Title = "Payment Requested",
            Body = $"{tableInfo} has requested the bill.",
            Priority = nameof(NotificationPriority.High),
            RequireAck = true,
            SoundKey = "notification_high",
            ActionUrl = "/dashboard/orders",
            EntityType = "Order",
            EntityId = orderId.ToString(),
            Metadata = new Dictionary<string, object>
            {
                ["orderId"] = orderId.ToString(),
                ["tableCode"] = tableCode ?? ""
            },
            TargetPermissions = new List<string> { Core.Data.Permissions.ProcessPayment }
        }, cancellationToken);

        _logger.LogInformation("Payment request notification sent for order {OrderId}, table {TableCode}", orderId, tableCode);

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Code = 200,
            UserMessage = "Payment request sent. A server will be with you shortly.",
            Data = new { },
            ServerTime = DateTimeOffset.UtcNow
        });
    }
}
