using API.Models;
using Core.DTO.Reservation;
using Core.Interface.Service.Reservation;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [Route("api/manual")]
    [ApiController]
    public class ManualReservationController : ControllerBase
    {
        private readonly IAdminReservationService _reservationService;
        private readonly ILogger<ManualReservationController> _logger;

        public ManualReservationController(
            IAdminReservationService reservationService,
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
        [ProducesResponseType(typeof(ApiResponse<List<ManualTableOptionDto>>), StatusCodes.Status200OK)]
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

            return Ok(new ApiResponse<List<ManualTableOptionDto>>
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

        /// <summary>
        /// Update reservation status.
        /// </summary>
        [HttpPatch("reservations/{id}/status")]
        public async Task<IActionResult> UpdateReservationStatus(
            long id,
            [FromBody] UpdateReservationStatusRequest request,
            CancellationToken ct)
        {
            var staffId = long.Parse(User.FindFirst("user_id")!.Value);

            var result = await _reservationService.UpdateReservationStatusAsync(
                id,
                staffId,
                request,
                ct);

            return Ok(new ApiResponse<ReservationStatusResponseDTO>
            {
                Success = true,
                Data = result,
                Code = 200,
                ServerTime = DateTimeOffset.UtcNow
            });
        }
    }
}
