using Core.DTO.Reservation;
using Core.Entity;

namespace Core.Interface.Repo;

/// <summary>
/// Repository interface for reservation data access operations.
/// </summary>
public interface IReservationRepository
{
    /// <summary>
    /// Creates a new reservation.
    /// </summary>
    /// <param name="reservation">The reservation entity to create</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The created reservation with ID</returns>
    Task<Reservation> CreateAsync(Reservation reservation, CancellationToken ct = default);

    /// <summary>
    /// Gets a reservation by its ID.
    /// </summary>
    /// <param name="reservationId">The reservation ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The reservation entity or null if not found</returns>
    Task<Reservation?> GetByIdAsync(long reservationId, CancellationToken ct = default);

    /// <summary>
    /// Gets existing reservations for a table at a specific time.
    /// Used to check for conflicts.
    /// </summary>
    /// <param name="tableId">The table ID</param>
    /// <param name="reservedTime">The reservation time</param>
    /// <param name="durationMinutes">Duration window to check (default 120 minutes)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of conflicting reservations</returns>
    Task<List<Reservation>> GetTableReservationsForTimeAsync(
        long tableId, 
        DateTime reservedTime, 
        int durationMinutes = 120,
        CancellationToken ct = default);

    /// <summary>
    /// Lấy danh sách Reservation có phân trang, lọc theo ngày và trạng thái.
    /// </summary>
    Task<(List<ReservationManagementDto> Items, int TotalCount)> GetReservationsAsync(
        GetReservationsRequest request,
        CancellationToken cancellationToken = default);
    // Thêm vào interface repo
    Task<List<LookupValue>> GetReservationStatusesAsync(CancellationToken cancellationToken = default);
}
