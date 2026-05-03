using API.Models;
using Core.DTO.Customer;
using Core.DTO.Notification;
using Core.DTO.Reservation;
using Core.Extensions;
using Core.Enum;
using Core.Interface.Service.Customer;
using Core.Interface.Service.Entity;
using Core.Interface.Service.Notification;
using Core.Interface.Service.Reservation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

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
    private const string PhoneLookupRateLimitPolicy = "PublicReservationPhoneLookup";
    private const string FitCheckRateLimitPolicy = "PublicReservationFitCheck";
    private const string CreateReservationRateLimitPolicy = "PublicReservationCreate";

    private readonly IPublicReservationService _reservationService;
    private readonly ICustomerService _customerService;
    private readonly ITableService _tableService;
    private readonly INotificationService _notificationService;
    private readonly IOrderService _orderService;
    private readonly ILogger<PublicReservationController> _logger;
    private readonly PublicReservationOptions _publicReservationOptions;

    public PublicReservationController(
        IPublicReservationService reservationService,
        ICustomerService customerService,
        ITableService tableService,
        INotificationService notificationService,
        IOrderService orderService,
        IOptions<PublicReservationOptions> publicReservationOptions,
        ILogger<PublicReservationController> logger)
    {
        _reservationService = reservationService;
        _customerService = customerService;
        _tableService = tableService;
        _notificationService = notificationService;
        _orderService = orderService;
        _publicReservationOptions = publicReservationOptions.Value;
        _logger = logger;
    }

    [HttpGet("customers/phone/{phone}")]
    [EnableRateLimiting(PhoneLookupRateLimitPolicy)]
    [ProducesResponseType(typeof(ApiResponse<PublicCustomerLookupDto>), StatusCodes.Status200OK)]
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

        return Ok(new ApiResponse<PublicCustomerLookupDto>
        {
            Success = true,
            Code = 200,
            UserMessage = "Customer retrieved successfully.",
            Data = new PublicCustomerLookupDto
            {
                CustomerId = customer.CustomerId,
                Phone = customer.Phone,
                MaskedName = MaskName(customer.FullName),
                MaskedEmail = MaskEmail(customer.Email)
            },
            ServerTime = DateTimeOffset.UtcNow
        });
    }

    [HttpPost("reservations/fit")]
    [EnableRateLimiting(FitCheckRateLimitPolicy)]
    [ProducesResponseType(typeof(ApiResponse<ReservationFitCheckResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckReservationFit(
        [FromBody] ReservationFitCheckRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _reservationService.CheckReservationFitAsync(request, cancellationToken);
        result.BookingToken = result.CanBookOnline
            ? GenerateBookingToken(request.ReservedTime, request.PartySize)
            : null;

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
    [EnableRateLimiting(CreateReservationRateLimitPolicy)]
    [ProducesResponseType(typeof(ApiResponse<ReservationResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateReservation(
        [FromBody] CreateReservationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryValidateBookingToken(request.ReservedTime, request.PartySize, request.BookingToken, out var tokenError))
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Code = 400,
                SubCode = 12,
                UserMessage = tokenError,
                SystemMessage = "BOOKING_TOKEN_INVALID",
                ValidateInfo = new List<string> { tokenError },
                Data = new { },
                ServerTime = DateTimeOffset.UtcNow
            });
        }

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
        var activeOrderId = await _tableService.OccupyTableByCodeAsync(tableCode, token, cancellationToken);

        _logger.LogInformation("Table {TableCode} accessed by customer via QR, activeOrderId={ActiveOrderId}", tableCode, activeOrderId);

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Code = 200,
            UserMessage = "Table accessed successfully.",
            SystemMessage = "Table validated",
            Data = new { activeOrderId },
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

    private string GenerateBookingToken(DateTime reservedTime, int partySize)
    {
        var normalizedReservedTime = NormalizeUtc(reservedTime);
        var expiresAtUnix = DateTimeOffset.UtcNow
            .AddMinutes(Math.Max(1, _publicReservationOptions.BookingToken.LifetimeMinutes))
            .ToUnixTimeSeconds();

        var payload = $"{normalizedReservedTime.Ticks}:{partySize}:{expiresAtUnix}";
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        var signatureBytes = ComputeSignature(payloadBytes);

        return $"{Base64UrlEncode(payloadBytes)}.{Base64UrlEncode(signatureBytes)}";
    }

    private bool TryValidateBookingToken(
        DateTime reservedTime,
        int partySize,
        string? bookingToken,
        out string errorMessage)
    {
        errorMessage = "Reservation verification expired. Please check availability again.";

        if (string.IsNullOrWhiteSpace(bookingToken))
        {
            return false;
        }

        var tokenParts = bookingToken.Split('.', 2, StringSplitOptions.RemoveEmptyEntries);
        if (tokenParts.Length != 2)
        {
            return false;
        }

        try
        {
            var payloadBytes = Base64UrlDecode(tokenParts[0]);
            var signatureBytes = Base64UrlDecode(tokenParts[1]);
            if (payloadBytes == null || signatureBytes == null)
            {
                return false;
            }

            var expectedSignature = ComputeSignature(payloadBytes);
            if (!CryptographicOperations.FixedTimeEquals(signatureBytes, expectedSignature))
            {
                return false;
            }

            var payload = Encoding.UTF8.GetString(payloadBytes);
            var payloadParts = payload.Split(':', 3, StringSplitOptions.RemoveEmptyEntries);
            if (payloadParts.Length != 3)
            {
                return false;
            }

            if (!long.TryParse(payloadParts[0], out var reservedTimeTicks) ||
                !int.TryParse(payloadParts[1], out var tokenPartySize) ||
                !long.TryParse(payloadParts[2], out var expiresAtUnix))
            {
                return false;
            }

            if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expiresAtUnix)
            {
                return false;
            }

            var normalizedReservedTime = NormalizeUtc(reservedTime);
            return reservedTimeTicks == normalizedReservedTime.Ticks && tokenPartySize == partySize;
        }
        catch
        {
            return false;
        }
    }

    private byte[] ComputeSignature(byte[] payloadBytes)
    {
        var secret = _publicReservationOptions.BookingToken.Secret;
        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new InvalidOperationException("PublicReservation:BookingToken:Secret must be configured.");
        }

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return hmac.ComputeHash(payloadBytes);
    }

    private static DateTime NormalizeUtc(DateTime value)
    {
        var normalized = value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
            : value;

        return normalized.ToUniversalTime();
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static byte[]? Base64UrlDecode(string input)
    {
        var padded = input.Replace('-', '+').Replace('_', '/');
        padded += (padded.Length % 4) switch
        {
            2 => "==",
            3 => "=",
            0 => string.Empty,
            _ => string.Empty
        };

        try
        {
            return Convert.FromBase64String(padded);
        }
        catch
        {
            return null;
        }
    }

    private static string? MaskName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return null;
        }

        var parts = fullName
            .Trim()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(MaskWord)
            .ToArray();

        return parts.Length == 0 ? null : string.Join(' ', parts);
    }

    private static string? MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var trimmed = email.Trim();
        var atIndex = trimmed.IndexOf('@');
        if (atIndex <= 0 || atIndex == trimmed.Length - 1)
        {
            return "***";
        }

        var local = trimmed[..atIndex];
        var domain = trimmed[(atIndex + 1)..];
        var dotIndex = domain.IndexOf('.');

        var maskedLocal = local.Length switch
        {
            <= 1 => "*",
            2 => $"{local[0]}*",
            _ => $"{local[..2]}***"
        };

        if (dotIndex <= 0 || dotIndex == domain.Length - 1)
        {
            return $"{maskedLocal}@***";
        }

        var host = domain[..dotIndex];
        var suffix = domain[dotIndex..];
        var maskedHost = host.Length <= 1 ? "*" : $"{host[0]}***";

        return $"{maskedLocal}@{maskedHost}{suffix}";
    }

    private static string MaskWord(string word)
    {
        if (string.IsNullOrWhiteSpace(word))
        {
            return string.Empty;
        }

        return word.Length switch
        {
            1 => "*",
            2 => $"{word[0]}*",
            _ => $"{word[0]}{new string('*', word.Length - 1)}"
        };
    }
}
