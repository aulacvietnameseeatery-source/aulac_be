using API.Models;
using Core.DTO.Reservation;
using Core.Interface.Service.Entity;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Asn1.Ocsp;

namespace Api.Controllers
{
    [Route("api/manual")]
    [ApiController]
    public class ManualReservationController : ControllerBase
    {
        private readonly IPublicReservationService _reservationService;
        private readonly ILogger<ManualReservationController> _logger;

        public ManualReservationController(
            IPublicReservationService reservationService,
            ILogger<ManualReservationController> logger)
        {
            _reservationService = reservationService;
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
        [HttpGet("table/availability")]
        [ProducesResponseType(typeof(ApiResponse<List<ManualTableAvailabilityDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAvailability(
            [FromQuery] DateTime? reservedTime,
            [FromQuery] int? partySize,
            CancellationToken cancellationToken = default)
        {
            var tables = await _reservationService.GetManualAvailableTablesAsync(
                reservedTime, partySize, cancellationToken);

            _logger.LogInformation(
                "Reserved Time {reservedTime}",
                reservedTime);

            return Ok(new ApiResponse<List<ManualTableAvailabilityDto>>
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
        /// Creates a soft lock on a table for 10 minutes.
        /// </summary>
        /// <param name="request">Lock request details</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Lock token and expiry information</returns>
        /// <response code="200">Table locked successfully</response>
        /// <response code="404">Table not found</response>
        /// <response code="409">Table already locked or has existing reservation</response>
        [HttpPost("reservations/lock")]
        [ProducesResponseType(typeof(ApiResponse<ReservationLockResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> LockTable(
            [FromBody] CreateReservationLockRequest request,
            CancellationToken cancellationToken = default)
        {
            var result = await _reservationService.LockTableAsync(request, cancellationToken);

            _logger.LogInformation(
                "Table {TableId} locked by {CustomerName} until {ExpiresAt}",
                result.TableId, request.CustomerName, result.ExpiresAt);

            return Ok(new ApiResponse<ReservationLockResponseDto>
            {
                Success = true,
                Code = 200,
                UserMessage = $"Table {result.TableCode} locked for 10 minutes.",
                SystemMessage = "Lock created successfully",
                Data = result,
                ServerTime = DateTimeOffset.UtcNow
            });
        }

        /// <summary>
        /// Submits a final reservation using a valid lock token.
        /// </summary>
        /// <param name="request">Reservation details with lock token</param>
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
            [FromBody] CreateManualReservationRequest request,
            CancellationToken cancellationToken = default)
        {
            var result = await _reservationService.CreateManualReservationAsync(request, cancellationToken);

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
    }
}
