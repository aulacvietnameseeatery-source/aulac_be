using Core.DTO.Reservation;

namespace Core.Interface.Service.Entity;

/// <summary>
/// Service interface for manual/admin reservation operations.
/// </summary>
public interface IManualReservationService
{
    Task<List<ManualTableOptionDto>> GetManualAvailableTablesAsync(
        DateTime? reservedTime,
        int? partySize,
        CancellationToken ct = default);

    Task<ReservationResponseDto> CreateManualReservationAsync(
        CreateManualReservationRequest request,
        CancellationToken ct = default);

    Task<ReservationStatusResponseDTO> UpdateReservationStatusAsync(
        long reservationId,
        long staffId,
        UpdateReservationStatusRequest request,
        CancellationToken ct);
}
