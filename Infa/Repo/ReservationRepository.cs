using Core.DTO.Reservation;
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

    public ReservationRepository(RestaurantMgmtContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Reservation> CreateAsync(Reservation reservation, CancellationToken ct = default)
    {
        reservation.CreatedAt = DateTime.UtcNow;

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
        int durationMinutes,
        uint cancelledStatusId,
        uint noShowStatusId,
        uint completedStatusId,
        CancellationToken ct = default)
    {
        // New reservation will occupy: [reservedTime, reservedTime + durationMinutes]
        // Check for overlapping existing reservations
        // Two time ranges overlap if:
        // - Range A starts before Range B ends AND
        // - Range A ends after Range B starts
        
        var newReservationEnd = reservedTime.AddMinutes(durationMinutes);

        return await _context.Reservations
            .AsNoTracking()
            .Include(r => r.Tables)
            .Where(r => r.Tables.Any(t => t.TableId == tableId))
            .Where(r => r.ReservationStatusLvId != cancelledStatusId)
            .Where(r => r.ReservationStatusLvId != noShowStatusId)
            .Where(r => r.ReservationStatusLvId != completedStatusId)
            // Overlap check: existing reservation overlaps with new reservation window
            // Existing: [r.ReservedTime, r.ReservedTime + duration]
            // New: [reservedTime, reservedTime + duration]
            .Where(r => 
                r.ReservedTime < newReservationEnd &&
                r.ReservedTime.AddMinutes(durationMinutes) > reservedTime
            )
            .ToListAsync(ct);
    }

    public async Task<(List<ReservationManagementDto> Items, int TotalCount)> GetReservationsAsync(
    GetReservationsRequest request,
    CancellationToken cancellationToken = default)
    {
        // 1. Khởi tạo Query
        var query = _context.Reservations
            .Include(r => r.ReservationStatusLv)
            .Include(r => r.Tables)
            .AsNoTracking()
            .AsQueryable();

        // 2. Filter theo Ngày (Date)
        if (request.Date.HasValue)
        {
            var filterDate = request.Date.Value.Date;
            query = query.Where(r => r.ReservedTime.Date == filterDate);
        }

        // 3. Filter theo Trạng thái (Tabs)
        if (request.StatusId.HasValue)
        {
            var statusId = request.StatusId.Value;
            query = query.Where(r => r.ReservationStatusLvId == statusId);
        }

        // 4. Search (Tên khách, SĐT)
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(r =>
                r.CustomerName.ToLower().Contains(search) ||
                r.Phone.Contains(search)
            );
        }

        if (request.TableId.HasValue)
        {
            query = query.Where(r => r.Tables.Any(t => t.TableId == request.TableId.Value));
        }

        // ----------------------------------------------------
        // 4.2 Filter  (CreatorId / StaffId)
        // ----------------------------------------------------
        if (request.CreatorId.HasValue)
        {
            
            // query = query.Where(r => r.CreatedBy == request.CreatorId.Value); 
        }

        if (!string.IsNullOrWhiteSpace(request.SortBy))
        {
            if (request.SortBy.Equals("reservedDate", StringComparison.OrdinalIgnoreCase))
            {
                query = query.OrderByDescending(r => r.ReservedTime);
            }
            else
            {
                query = query.OrderByDescending(r => r.CreatedAt ?? DateTime.MinValue);
            }
        }
        else
        {
            query = query.OrderByDescending(r => r.CreatedAt ?? DateTime.MinValue);
        }

        query = ((IOrderedQueryable<Reservation>)query).ThenByDescending(r => r.ReservationId);

        // 6. Pagination
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.PageIndex - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new ReservationManagementDto
            {
                ReservationId = r.ReservationId,
                ReservedTime = DateTime.SpecifyKind(r.ReservedTime, DateTimeKind.Utc),
                CustomerName = r.CustomerName,
                Phone = r.Phone,
                Email = r.Email,
                Pax = r.PartySize,
                StatusId = r.ReservationStatusLvId,
                StatusName = r.ReservationStatusLv.ValueName,
                TableName = string.Join(", ", r.Tables.Select(t => t.TableCode)),
                PreOrderSummary = null,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<List<LookupValue>> GetReservationStatusesAsync(CancellationToken cancellationToken = default)
    {
        // Gọi đúng namespace Core.Enum.LookupType để tránh lỗi trùng tên Entity
        var typeId = (long)Core.Enum.LookupType.ReservationStatus;

        return await _context.LookupValues
            .Where(lv => lv.TypeId == typeId
                         && lv.IsActive == true
                         && lv.DeletedAt == null)
            .OrderBy(lv => lv.ValueId) 
            .ToListAsync(cancellationToken);
    }

    public async Task<Reservation?> GetByIdWithTablesAsync(
        long id,
        CancellationToken ct)
    {
        return await _context.Reservations
            .Include(r => r.Tables)
            .Include(r => r.ReservationStatusLv)
            .FirstOrDefaultAsync(r => r.ReservationId == id, ct);
    }

    public async Task UpdateAsync(
        Reservation reservation,
        CancellationToken ct)
    {
        _context.Reservations.Update(reservation);

        await _context.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task<Reservation?> GetByIdWithFullDetailsAsync(long reservationId, CancellationToken ct = default)
    {
        return await _context.Reservations
            .Include(r => r.Tables).ThenInclude(t => t.TableTypeLv)
            .Include(r => r.Tables).ThenInclude(t => t.ZoneLv)
            .Include(r => r.ReservationStatusLv)
            .Include(r => r.SourceLv)
            .FirstOrDefaultAsync(r => r.ReservationId == reservationId, ct);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Reservation reservation, CancellationToken ct = default)
    {
        _context.Reservations.Remove(reservation);
        await _context.SaveChangesAsync(ct);
    }
}
