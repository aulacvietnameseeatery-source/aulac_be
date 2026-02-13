using Core.DTO.DishCategory;
using Core.DTO.General;
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
    public async Task<PagedResultDTO<DishCategoryDto>> GetAllCategoriesAsync(DishCategoryListQueryDTO query, CancellationToken cancellationToken = default)
    {
        var dbQuery = _context.DishCategories.AsQueryable();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var searchLower = query.Search.ToLower();
            dbQuery = dbQuery.Where(c => 
                c.CategoryName.ToLower().Contains(searchLower) ||
                (c.Description != null && c.Description.ToLower().Contains(searchLower))
            );
        }

        // Apply status filter
        if (query.IsDisabled.HasValue)
        {
            dbQuery = dbQuery.Where(c => c.IsDisabled == query.IsDisabled.Value);
        }

        // Get total count
        var totalCount = await dbQuery.CountAsync(cancellationToken);

        // Apply pagination
        var categories = await dbQuery
            .OrderBy(c => c.CategoryId)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(c => new DishCategoryDto
            {
                CategoryId = c.CategoryId,
                CategoryName = c.CategoryName,
                Description = c.Description,
                IsDisabled = c.IsDisabled
            })
            .ToListAsync(cancellationToken);

        return new PagedResultDTO<DishCategoryDto>
        {
            PageData = categories,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };
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
