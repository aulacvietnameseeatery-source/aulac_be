using Core.DTO.Reservation;

namespace Core.Interface.Service.Reservation;

/// <summary>
/// Service interface for public reservation operations.
/// </summary>
public interface IPublicReservationService
{
    /// <summary>
    /// Gets available tables for reservation.
    /// </summary>
    /// <param name="reservedTime">Optional filter by reservation time</param>
    /// <param name="partySize">Optional filter by minimum capacity</param>
    /// <param name="zone">Optional filter by zone</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of available tables</returns>
    Task<List<TableAvailabilityDto>> GetAvailableTablesAsync(
        DateTime? reservedTime,
        int? partySize,
        string? zone,
        CancellationToken ct = default);

    Task<ReservationFitCheckResponse> CheckReservationFitAsync(
        ReservationFitCheckRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Submits a final reservation.
    /// </summary>
    /// <param name="request">Reservation details with lock token</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Confirmed reservation details</returns>
    Task<ReservationResponseDto> CreateReservationAsync(
        CreateReservationRequest request,
        CancellationToken ct = default);

}
