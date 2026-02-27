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
        // Get tables that are not under maintenance (LOCKED)
        // and are online
        return await _context.RestaurantTables
            .AsNoTracking()
            .Include(t => t.TableTypeLv)
            .Include(t => t.ZoneLv)
            .Where(t => t.TableStatusLvId != TableStatusLocked)
            .Where(t => t.IsOnline == true)
            .OrderBy(t => t.Capacity)
            .ThenBy(t => t.TableCode)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<RestaurantTable?> GetByIdAsync(long tableId, CancellationToken ct = default)
    {
        return await _context.RestaurantTables
            .Include(t => t.TableTypeLv)
            .Include(t => t.ZoneLv)
            .FirstOrDefaultAsync(t => t.TableId == tableId, ct);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(long tableId, CancellationToken ct = default)
    {
        return await _context.RestaurantTables
            .AsNoTracking()
            .AnyAsync(t => t.TableId == tableId, ct);
    }

    public async Task<List<RestaurantTable>> GetManualAvailableTablesAsync(CancellationToken ct = default)
    {
        // Get tables that are not under maintenance (LOCKED)
        return await _context.RestaurantTables
            .AsNoTracking()
            .Include(t => t.TableTypeLv)
            .Include(t => t.ZoneLv)
            .Where(t => t.TableStatusLvId != TableStatusLocked)
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
            .AsNoTracking()
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

    public async Task<List<RestaurantTable>> GetTablesWithRelationsAsync(CancellationToken ct = default)
    {
        return await _context.RestaurantTables
            .Include(t => t.TableStatusLv)
            .Include(t => t.ZoneLv)
            .Include(t => t.Orders)
                .ThenInclude(o => o.OrderStatusLv)
            .Include(t => t.Reservations)
            .AsQueryable().ToListAsync(ct);
    }

    public async Task UpdateAsync(RestaurantTable table, CancellationToken ct)
    {
        _context.RestaurantTables.Update(table);
        await _context.SaveChangesAsync(ct);
    }
}
