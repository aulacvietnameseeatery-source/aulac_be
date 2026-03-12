using API.Models;
using Core.DTO.Reservation;
using Core.Interface.Service.Entity;
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
    private readonly ITableService _tableService;
    private readonly ILogger<PublicReservationController> _logger;

    public PublicReservationController(
        IPublicReservationService reservationService,
        ITableService tableService,
        ILogger<PublicReservationController> logger)
    {
        _reservationService = reservationService;
        _tableService = tableService;
        _logger = logger;
    }

    /// <summary>
    /// Gets available tables for reservation.
    /// </summary>
    /// <param name="reservedTime">Optional filter by reservation time</param>
    /// <param name="partySize">Optional filter by minimum capacity</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of available tables</returns>
    /// <response code="200">Returns list of available tables</response>
    [HttpGet("availability")]
    [ProducesResponseType(typeof(ApiResponse<List<TableAvailabilityDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAvailability(
        [FromQuery] DateTime? reservedTime,
        [FromQuery] int? partySize,
        [FromQuery] string? zone,
        CancellationToken cancellationToken = default)
    {
        var tables = await _reservationService.GetAvailableTablesAsync(
            reservedTime, partySize, zone, cancellationToken);

        return Ok(new ApiResponse<List<TableAvailabilityDto>>
        {
            Success = true,
            Code = 200,
            UserMessage = $"Found {tables.Count} tables.",
            SystemMessage = "Availability check successful",
            Data = tables,
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
            UserMessage = "Reservation confirmed successfully.",
            SystemMessage = "Reservation created",
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
}
