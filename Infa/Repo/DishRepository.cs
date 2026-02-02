using Core.Entity;
using Core.Interface.Repo;
using Infa.Data;
using Microsoft.EntityFrameworkCore;

namespace Infa.Repo;

/// <summary>
/// Repository implementation for dish data access operations.
/// </summary>
public class DishRepository : IDishRepository
{
    private readonly RestaurantMgmtContext _context;

    public DishRepository(RestaurantMgmtContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Dish?> FindByIdAsync(long dishId, CancellationToken ct = default)
    {
        return await _context.Dishes
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.DishId == dishId, ct);
    }

    /// <inheritdoc />
    public async Task UpdateStatusAsync(long dishId, uint statusLvId, CancellationToken ct = default)
    {
        await _context.Dishes
            .Where(d => d.DishId == dishId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(d => d.DishStatusLvId, statusLvId),
                ct);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(long dishId, CancellationToken ct = default)
    {
        return await _context.Dishes
            .AsNoTracking()
            .AnyAsync(d => d.DishId == dishId, ct);
    }
}
