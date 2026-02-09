using Core.Entity;
using Core.Interface.Repo;
using Infa.Data;
using Microsoft.EntityFrameworkCore;

namespace Infa.Repo;

/// <summary>
/// Repository implementation for reservation data access operations.
/// </summary>
public class ReservationRepository : IReservationRepository
{
    private readonly RestaurantMgmtContext _context;

    // Reservation status lookup value IDs
    private const uint ReservationStatusPending = 21;
    private const uint ReservationStatusConfirmed = 22;
    private const uint ReservationStatusCheckedIn = 23;
    private const uint ReservationStatusCancelled = 24;
    private const uint ReservationStatusNoShow = 25;

    public ReservationRepository(RestaurantMgmtContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Reservation> CreateAsync(Reservation reservation, CancellationToken ct = default)
    {
        _context.Reservations.Add(reservation);
        await _context.SaveChangesAsync(ct);
        return reservation;
    }

    /// <inheritdoc />
    public async Task<Reservation?> GetByIdAsync(long reservationId, CancellationToken ct = default)
    {
        return await _context.Reservations
            .AsNoTracking()
            .Include(r => r.Tables)
            .Include(r => r.ReservationStatusLv)
            .FirstOrDefaultAsync(r => r.ReservationId == reservationId, ct);
    }

    /// <inheritdoc />
    public async Task<List<Reservation>> GetTableReservationsForTimeAsync(
        long tableId,
        DateTime reservedTime,
        int durationMinutes = 120,
        CancellationToken ct = default)
    {
        // Check for overlapping reservations within the time window
        // A reservation conflicts if:
        // - It's for the same table
        // - It's not cancelled or no-show
        // - The time windows overlap

        var startWindow = reservedTime.AddMinutes(-durationMinutes);
        var endWindow = reservedTime.AddMinutes(durationMinutes);

        return await _context.Reservations
            .AsNoTracking()
            .Include(r => r.Tables)
            .Where(r => r.Tables.Any(t => t.TableId == tableId))
            .Where(r => r.ReservationStatusLvId != ReservationStatusCancelled)
            .Where(r => r.ReservationStatusLvId != ReservationStatusNoShow)
            .Where(r => r.ReservedTime >= startWindow && r.ReservedTime <= endWindow)
            .ToListAsync(ct);
    }
}
