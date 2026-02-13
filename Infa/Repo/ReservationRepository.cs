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
            .Where(r => r.ReservationStatusLvId != ReservationStatusCancelled)
            .Where(r => r.ReservationStatusLvId != ReservationStatusNoShow)
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
            .Include(r => r.ReservationStatusLv) // Join bảng Lookup để lấy Status Name
            .AsNoTracking()
            .AsQueryable();

        // 2. Filter theo Ngày (Date)
        // Mặc định nếu không truyền Date thì có thể lấy tất cả hoặc logic tùy chọn. 
        // Ở đây ta check HasValue như yêu cầu.
        if (request.Date.HasValue)
        {
            var filterDate = request.Date.Value.Date;
            // So sánh phần ngày: ReservedTime >= 00:00 và < 23:59 của ngày đó
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

        // 5. Sorting (Mặc định sắp xếp theo giờ đặt tăng dần - Sớm nhất lên đầu)
        query = query.OrderBy(r => r.ReservedTime);

        // 6. Pagination
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.PageIndex - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new ReservationManagementDto
            {
                ReservationId = r.ReservationId,
                ReservedTime = r.ReservedTime,
                CustomerName = r.CustomerName,
                Phone = r.Phone,
                Email = r.Email,
                Pax = r.PartySize,

                // Map Status
                StatusId = r.ReservationStatusLvId,
                StatusName = r.ReservationStatusLv.ValueName,

                // Pre-order: Tạm thời để null vì chưa có liên kết DB
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
}
