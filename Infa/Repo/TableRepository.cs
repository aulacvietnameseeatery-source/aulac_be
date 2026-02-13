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
}
