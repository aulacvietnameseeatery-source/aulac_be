using Core.DTO.Table;
using Core.Entity;
using Core.Interface.Repo;
using Infa.Data;
using Microsoft.EntityFrameworkCore;

namespace Infa.Repo;

/// <summary>
/// Repository implementation for restaurant table data access operations.
/// </summary>
public class TableRepository : ITableRepository
{
    private readonly RestaurantMgmtContext _context;

    // Table status lookup value IDs
    private const uint TableStatusAvailable = 14;
    private const uint TableStatusOccupied = 15;
    private const uint TableStatusReserved = 16;
    private const uint TableStatusLocked = 17; // Maintenance

    public TableRepository(RestaurantMgmtContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<List<RestaurantTable>> GetAvailableTablesAsync(CancellationToken ct = default)
    {
        return await _context.RestaurantTables
            .AsNoTracking()
            .Include(t => t.TableTypeLv)
            .Include(t => t.ZoneLv)
            .Where(t => !t.IsDeleted)
            .Where(t => t.TableStatusLvId != TableStatusLocked)
            .Where(t => t.IsOnline == true)
            .OrderBy(t => t.Capacity)
                .ThenBy(t => t.TableCode)
            .ToListAsync(ct);
    }

    public async Task<List<RestaurantTable>> GetAvailableTablesAsync(DateTime targetTime, CancellationToken ct = default)
    {
        // Giả sử mỗi bữa ăn kéo dài 2 tiếng. Bàn bị kẹt từ (Giờ khách đến - 2 tiếng) đến (Giờ khách đến + 2 tiếng)
        var diningDuration = TimeSpan.FromHours(2);
        var startTime = targetTime.Add(-diningDuration); // Ví dụ khách đặt 19h -> Tính từ 17h
        var endTime = targetTime.Add(diningDuration);    // Đến 21h

        // 1. Tìm ID của các bàn ĐANG BỊ KẸT LỊCH
        var busyTableIds = await _context.Reservations
            .Where(r => r.ReservedTime > startTime && r.ReservedTime < endTime)
            .Where(r => r.ReservationStatusLv.ValueCode == "PENDING" ||
                        r.ReservationStatusLv.ValueCode == "CONFIRMED" ||
                        r.ReservationStatusLv.ValueCode == "CHECKED_IN")
            .SelectMany(r => r.Tables.Select(t => t.TableId))
            .Distinct()
            .ToListAsync(ct);

        // 2. Lấy danh sách bàn CÒN TRỐNG (Loại trừ các bàn kẹt lịch)
        return await _context.RestaurantTables
            .AsNoTracking()
            .Include(t => t.TableTypeLv)
            .Include(t => t.ZoneLv)
            .Where(t => !t.IsDeleted)
            .Where(t => t.TableStatusLvId != TableStatusLocked)
            .Where(t => t.IsOnline == true)
            .Where(t => !busyTableIds.Contains(t.TableId))
            .OrderBy(t => t.ZoneLv.ValueName) // Xếp theo Zone 
            .ThenBy(t => t.Capacity)
            .ThenBy(t => t.TableCode)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<RestaurantTable?> GetByIdAsync(long tableId, CancellationToken ct = default)
    {
        return await _context.RestaurantTables
            .Include(t => t.TableStatusLv)
            .Include(t => t.TableTypeLv)
            .Include(t => t.ZoneLv)
            .FirstOrDefaultAsync(t => t.TableId == tableId && !t.IsDeleted, ct);
    }

    /// <inheritdoc />
    public async Task<RestaurantTable?> GetByIdWithDetailsAsync(long tableId, CancellationToken ct = default)
    {
        return await _context.RestaurantTables
            .Include(t => t.TableStatusLv)
            .Include(t => t.TableTypeLv)
            .Include(t => t.ZoneLv)
            .Include(t => t.TableMedia)
                .ThenInclude(tm => tm.Media)
            .Include(t => t.TableQrImgNavigation)
            .Include(t => t.Orders)
                .ThenInclude(o => o.OrderStatusLv)
            .Include(t => t.Reservations)
                .ThenInclude(r => r.ReservationStatusLv)
            .Include(t => t.ServiceErrors)
            .FirstOrDefaultAsync(t => t.TableId == tableId && !t.IsDeleted, ct);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(long tableId, CancellationToken ct = default)
    {
        return await _context.RestaurantTables
            .AsNoTracking()
            .AnyAsync(t => t.TableId == tableId && !t.IsDeleted, ct);
    }

    /// <inheritdoc />
    public async Task<bool> TableCodeExistsAsync(string code, long? excludeId = null, CancellationToken ct = default)
    {
        var query = _context.RestaurantTables
            .AsNoTracking()
            .Where(t => t.TableCode == code && !t.IsDeleted);

        if (excludeId.HasValue)
            query = query.Where(t => t.TableId != excludeId.Value);

        return await query.AnyAsync(ct);
    }

    /// <inheritdoc />
    public async Task<int> CountActiveOrdersAsync(long tableId, CancellationToken ct = default)
    {
        return await _context.Orders
            .AsNoTracking()
            .Where(o => o.TableId == tableId)
            .Where(o => o.OrderStatusLv.ValueCode == nameof(OrderStatusCode.PENDING)
                      || o.OrderStatusLv.ValueCode == nameof(OrderStatusCode.IN_PROGRESS))
            .CountAsync(ct);
    }

    /// <inheritdoc />
    public async Task<int> CountUpcomingReservationsAsync(long tableId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        return await _context.Reservations
            .AsNoTracking()
            .Where(r => r.Tables.Any(t => t.TableId == tableId))
            .Where(r => r.ReservedTime > now)
            .Where(r => r.ReservationStatusLv.ValueCode == nameof(ReservationStatusCode.PENDING)
                      || r.ReservationStatusLv.ValueCode == nameof(ReservationStatusCode.CONFIRMED))
            .CountAsync(ct);
    }

    public async Task<List<RestaurantTable>> GetManualAvailableTablesAsync(CancellationToken ct = default)
    {
        return await _context.RestaurantTables
            .AsNoTracking()
            .Include(t => t.TableTypeLv)
            .Include(t => t.ZoneLv)
            .Where(t => !t.IsDeleted)
            .Where(t => t.TableStatusLvId != TableStatusLocked && t.TableStatusLvId != TableStatusOccupied)
            .OrderBy(t => t.Capacity)
                .ThenBy(t => t.TableCode)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<(List<RestaurantTable> Items, int TotalCount)> GetTablesForManagementAsync(GetTableManagementRequest request, CancellationToken ct = default)
    {
        var query = _context.RestaurantTables
            .Include(t => t.TableStatusLv)
            .Include(t => t.TableTypeLv)
            .Include(t => t.ZoneLv)
            .Include(t => t.TableMedia)
                .ThenInclude(tm => tm.Media)
            .Include(t => t.TableQrImgNavigation)
            .AsNoTracking()
            .Where(t => !t.IsDeleted)
            .AsQueryable();

        //  Search by table code
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(t => t.TableCode.ToLower().Contains(search));
        }

        //  Filter
        if (request.ZoneId.HasValue)
            query = query.Where(t => t.ZoneLvId == request.ZoneId.Value);

        if (request.TypeId.HasValue)
            query = query.Where(t => t.TableTypeLvId == request.TypeId.Value);

        if (request.StatusId.HasValue)
            query = query.Where(t => t.TableStatusLvId == request.StatusId.Value);

        if (request.IsOnline.HasValue)
            query = query.Where(t => t.IsOnline == request.IsOnline.Value);

        // TÌM BÀN TRỐNG THEO GIỜ CỤ THỂ
        if (request.TargetTime.HasValue)
        {
            var targetTime = request.TargetTime.Value;
            var diningDuration = TimeSpan.FromHours(2);
            var startTime = targetTime.Add(-diningDuration);
            var endTime = targetTime.Add(diningDuration);

            // Tìm ID của các bàn ĐANG BỊ KẸT LỊCH trong khoảng targetTime
            var busyTableIds = await _context.Reservations
                .Where(r => r.ReservedTime > startTime && r.ReservedTime < endTime)
                .Where(r => r.ReservationStatusLv.ValueCode == "PENDING" ||
                            r.ReservationStatusLv.ValueCode == "CONFIRMED" ||
                            r.ReservationStatusLv.ValueCode == "CHECKED_IN")
                .SelectMany(r => r.Tables.Select(t => t.TableId))
                .Distinct()
                .ToListAsync(ct);

            // Chỉ lấy những bàn không nằm trong list kẹt lịch
            query = query.Where(t => !busyTableIds.Contains(t.TableId));
        }

        var totalCount = await query.CountAsync(ct);

        var pageIndex = request.PageIndex < 1 ? 1 : request.PageIndex;
        var pageSize = request.PageSize < 1 ? 50 : request.PageSize;

        var items = await query
            .OrderBy(t => t.TableCode)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }


    /// <inheritdoc />
    public async Task UpdateStatusAsync(long tableId, uint statusLvId, CancellationToken ct = default)
    {
        var table = await _context.RestaurantTables.FirstOrDefaultAsync(t => t.TableId == tableId, ct);

        if (table is null) return;

        table.TableStatusLvId = statusLvId;
        await _context.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task<RestaurantTable?> GetByCodeAsync(string tableCode, CancellationToken ct = default)
    {
        return await _context.RestaurantTables
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TableCode == tableCode, ct);
    }

    public async Task<List<RestaurantTable>> GetTablesWithRelationsAsync(CancellationToken ct = default)
    {
        return await _context.RestaurantTables
            .Include(t => t.TableStatusLv)
            .Include(t => t.ZoneLv)
            .Include(t => t.Orders)
                .ThenInclude(o => o.OrderStatusLv)
            .Include(t => t.Reservations)
            .Where(t => !t.IsDeleted)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public void Add(RestaurantTable table)
    {
        _context.RestaurantTables.Add(table);
    }

    public async Task UpdateAsync(RestaurantTable table, CancellationToken ct)
    {
        _context.RestaurantTables.Update(table);
        await _context.SaveChangesAsync(ct);

    }

    /// <inheritdoc />
    public async Task<bool> IsValidLookupAsync(uint valueId, ushort typeId, CancellationToken ct = default)
    {
        return await _context.LookupValues
            .AsNoTracking()
            .AnyAsync(lv => lv.ValueId == valueId && lv.TypeId == typeId && lv.IsActive == true && lv.DeletedAt == null, ct);
    }

    /// <inheritdoc />
    public async Task<string?> GetLookupValueCodeAsync(uint valueId, CancellationToken ct = default)
    {
        return await _context.LookupValues
           .AsNoTracking()
           .Where(lv => lv.ValueId == valueId && lv.IsActive == true && lv.DeletedAt == null)
           .Select(lv => lv.ValueCode)
           .FirstOrDefaultAsync(ct);
    }

    /// <inheritdoc />
    public async Task<int> BulkSetOnlineByZoneAsync(uint zoneLvId, bool isOnline, CancellationToken ct = default)
    {
        var tables = await _context.RestaurantTables
       .Where(t => t.ZoneLvId == zoneLvId && !t.IsDeleted)
            .ToListAsync(ct);

        foreach (var table in tables)
        {
            table.IsOnline = isOnline;
            table.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(ct);
        return tables.Count;
    }

    /// <inheritdoc />
    public async Task<TableMedium?> GetTableMediaAsync(long tableId, long mediaId, CancellationToken ct = default)
    {
        return await _context.TableMedia
               .Include(tm => tm.Media)
               .FirstOrDefaultAsync(tm => tm.TableId == tableId && tm.MediaId == mediaId, ct);
    }

    /// <inheritdoc />
    public void AddTableMedia(TableMedium tableMedium)
    {
        _context.TableMedia.Add(tableMedium);
    }

    /// <inheritdoc />
    public void RemoveTableMedia(TableMedium tableMedium)
    {
        _context.TableMedia.Remove(tableMedium);
    }

    /// <inheritdoc />
    public async Task SetQrImageAsync(long tableId, long mediaId, CancellationToken ct = default)
    {
        var table = await _context.RestaurantTables
  .FirstOrDefaultAsync(t => t.TableId == tableId, ct);

        if (table is not null)
            table.TableQrImg = mediaId;
    }

    public async Task<bool> TryOccupyIfAvailableAsync(
        long tableId,
        uint availableLvId,
        uint occupiedLvId,
        CancellationToken ct)
    {
        var affected = await _context.RestaurantTables
            .Where(t => t.TableId == tableId && t.TableStatusLvId == availableLvId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(t => t.TableStatusLvId, occupiedLvId)
                .SetProperty(t => t.UpdatedAt, DateTime.UtcNow),
                ct);

        return affected == 1;
    }
}