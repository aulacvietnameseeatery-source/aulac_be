using Core.Entity;
using Core.Interface.Repo;
using Infa.Data;
using Microsoft.EntityFrameworkCore;

namespace Infa.Repo;

/// <summary>
/// Repository implementation for dish category data access operations.
/// </summary>
public class DishCategoryRepository : IDishCategoryRepository
{
    private readonly RestaurantMgmtContext _context;

    public DishCategoryRepository(RestaurantMgmtContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<List<DishCategory>> GetAllAsync(bool includeDisabled = false, CancellationToken cancellationToken = default)
    {
        var query = _context.DishCategories.AsQueryable();

        if (!includeDisabled)
        {
            query = query.Where(c => !c.IsDisabled);
        }

        return await query
            .OrderBy(c => c.CategoryId)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<DishCategory?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _context.DishCategories
            .FirstOrDefaultAsync(c => c.CategoryId == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<DishCategory> CreateAsync(DishCategory category, CancellationToken cancellationToken = default)
    {
        _context.DishCategories.Add(category);
        await _context.SaveChangesAsync(cancellationToken);
        return category;
    }

    /// <inheritdoc />
    public async Task<DishCategory> UpdateAsync(DishCategory category, CancellationToken cancellationToken = default)
    {
        _context.DishCategories.Update(category);
        await _context.SaveChangesAsync(cancellationToken);
        return category;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var category = await GetByIdAsync(id, cancellationToken);
        if (category == null)
        {
            return false;
        }

        _context.DishCategories.Remove(category);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> HasDishesAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _context.Dishes
            .AnyAsync(d => d.CategoryId == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsByNameAsync(string name, long? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.DishCategories
            .Where(c => c.CategoryName.ToLower() == name.ToLower());

        if (excludeId.HasValue)
        {
            query = query.Where(c => c.CategoryId != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }
}
