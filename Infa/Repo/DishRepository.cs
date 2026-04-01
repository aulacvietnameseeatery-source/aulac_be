using Core.DTO.Dish;
using Core.Entity;
using Core.Enum;
using Core.Interface.Repo;
using Infa.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

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

    public async Task<(List<Dish> Items, int TotalCount)> GetDishesAsync(
    GetDishesRequest request,
    CancellationToken cancellationToken = default)
    {
        var query = _context.Dishes
            .Include(d => d.Category)
                .ThenInclude(c => c.CategoryNameText)
                    .ThenInclude(t => t.I18nTranslations)
            .Include(d => d.DishStatusLv)
            .Include(d => d.DishMedia)
                .ThenInclude(dm => dm.Media)
            .Include(d => d.DishNameText)
                .ThenInclude(t => t.I18nTranslations)
            .Include(d => d.DescriptionText)
                .ThenInclude(t => t.I18nTranslations)
            .AsNoTracking()
            .AsQueryable();

        // Logic Searching 
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchTerm = request.Search.Trim().ToLower();
            query = query.Where(d =>
                d.DishName.ToLower().Contains(searchTerm) ||
                (d.Description != null && d.Description.ToLower().Contains(searchTerm)) ||
                (d.Category != null && d.Category.CategoryName.ToLower().Contains(searchTerm))
            );
        }


        //  Filter Customer
        if (request.IsCustomerView)
        {
            var availableCode = DishStatusCode.AVAILABLE.ToString();
            var lookupTypeId = (long)Core.Enum.LookupType.DishStatus;

            var availableId = await _context.LookupValues
                .Where(lv => lv.TypeId == lookupTypeId && lv.ValueCode == availableCode)
                .Select(lv => lv.ValueId)
                .FirstOrDefaultAsync(cancellationToken);

            query = query.Where(d => d.IsOnline == true &&
                                     d.DishStatusLvId == availableId &&
                                     d.Category != null &&
                                     d.Category.IsDisabled == false);
        }
        else
        {
            // Filter Admin 
            if (request.Status.HasValue)
            {
                var statusId = (long)request.Status.Value;
                query = query.Where(d => d.DishStatusLvId == statusId);
            }
        }

        // Logic Sorting
        if (string.IsNullOrWhiteSpace(request.SortBy))
        {
            query = query.OrderByDescending(d => d.DishId);
        }
        else
        {
            switch (request.SortBy.ToLower())
            {
                case "price":
                    query = request.IsDescending
                        ? query.OrderByDescending(d => d.Price)
                        : query.OrderBy(d => d.Price);
                    break;
                case "name":
                    query = request.IsDescending
                        ? query.OrderByDescending(d => d.DishName)
                        : query.OrderBy(d => d.DishName);
                    break;
                default:
                    query = query.OrderByDescending(d => d.CreatedAt);
                    break;
            }
        }

        // Pagination 

        var totalCount = await query.CountAsync(cancellationToken);

        var pageIndex = request.PageIndex < 1 ? 1 : request.PageIndex;
        var pageSize = request.PageSize < 1 ? 10 : request.PageSize;

        var items = await query
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<List<string>> GetAllCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.DishCategories
            .Where(c => c.IsDisabled == false)
            .OrderBy(c => c.DisPlayOrder)
            .Select(c => c.CategoryName)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<LookupValue>> GetDishStatusesAsync(CancellationToken cancellationToken = default)
    {

        var typeId = (long)Core.Enum.LookupType.DishStatus;

        return await _context.LookupValues
            .Where(lv => lv.TypeId == typeId
                         && lv.IsActive == true
                         && lv.DeletedAt == null)
            .OrderBy(lv => lv.ValueId) 
            .ToListAsync(cancellationToken);
    }

    public async Task<Dish?> GetDishByIdAsync(long dishId, CancellationToken cancellationToken = default)
    {
        return await _context.Dishes
            .Include(d => d.Category)
                .ThenInclude(c => c.CategoryNameText)
                    .ThenInclude(t => t!.I18nTranslations)
            .Include(d => d.DishStatusLv)
            .Include(d => d.DishMedia)
                .ThenInclude(dm => dm.Media)
            .Include(d => d.Recipes)
                .ThenInclude(r => r.Ingredient)
                    .ThenInclude(i => i.IngredientNameText)
                        .ThenInclude(t => t!.I18nTranslations)
            .Include(d => d.DishNameText)
                .ThenInclude(t => t.I18nTranslations)
            .Include(d => d.DescriptionText)
                .ThenInclude(t => t!.I18nTranslations)
            .Include(d => d.ShortDescriptionText)
                .ThenInclude(t => t!.I18nTranslations)
            .Include(d => d.SloganText)
                .ThenInclude(t => t!.I18nTranslations)
            .FirstOrDefaultAsync(d => d.DishId == dishId, cancellationToken);
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

    public async Task AddAsync(Dish dish, CancellationToken ct)
    {
        _context.Dishes.Add(dish); // Add new Dish entity to the context
        await _context.SaveChangesAsync(ct); // Persist changes to the database
    }

    public async Task<Dish?> FindByIdForActionAsync(long id, CancellationToken ct)
    {
        // Retrieve a Dish by ID, including related entities for full details
        return await _context.Dishes
        .Include(x => x.DishNameText).ThenInclude(x => x.I18nTranslations)
        .Include(x => x.DescriptionText).ThenInclude(x => x.I18nTranslations)
        .Include(x => x.ShortDescriptionText).ThenInclude(x => x.I18nTranslations)
        .Include(x => x.SloganText).ThenInclude(x => x.I18nTranslations)
        .Include(x => x.NoteText).ThenInclude(x => x.I18nTranslations)
        .Include(x => x.DishStatusLv)
            .ThenInclude(s => s.ValueNameText)
                .ThenInclude(t => t.I18nTranslations)
        .Include(x => x.Category)
            .ThenInclude(c => c.CategoryNameText)
                .ThenInclude(t => t.I18nTranslations)
        .Include(x => x.DishMedia)
            .ThenInclude(x => x.Media)
                .ThenInclude(x => x.MediaTypeLv)
        .FirstOrDefaultAsync(x => x.DishId == id, ct); // Return the first match or null
    }

    public async Task<List<LookupValue>> GetActiveDishStatusEntitiesAsync()
    {
        // Get all active dish status lookup values
        return await _context.LookupValues
        .AsNoTracking() // No tracking for read-only query
        .Where(lv =>
            lv.TypeId == (ushort)Core.Enum.LookupType.DishStatus &&
            lv.IsActive == true &&
            lv.DeletedAt == null
        )
        .OrderBy(lv => lv.ValueId) // Order by ValueId
        .ToListAsync();
    }

    public async Task<List<DishCategory>> GetAllDishCategoriesAsync()
    {
        // Retrieve all dish categories, ordered by CategoryId
        return await _context.DishCategories
            .AsNoTracking()
            .Include(c => c.CategoryNameText)
                .ThenInclude(t => t.I18nTranslations)
            .Where(c => !c.IsDisabled)
            .OrderBy(c => c.CategoryId)
            .ToListAsync();
    }

    public async Task<List<LookupValue>> GetAllActiveTagsAsync()
    {
        return await _context.LookupValues
            .AsNoTracking()
            .Include(lv => lv.ValueNameText)
                .ThenInclude(t => t!.I18nTranslations)
            .Include(lv => lv.ValueDescText) 
                .ThenInclude(t => t!.I18nTranslations)
            .Where(lv =>
                lv.TypeId == (ushort)Core.Enum.LookupType.Tag &&
                lv.IsActive == true &&
                lv.DeletedAt == null
            )
            .OrderBy(lv => lv.SortOrder)
            .ToListAsync();
    }

    public async Task AddDishTagAsync(DishTag dishTag, CancellationToken ct)
    {
        _context.DishTags.Add(dishTag); // Add new DishTag entity to the context
        await _context.SaveChangesAsync(ct); // Persist changes to the database
    }

    public async Task<List<DishTag>> FindTagByDishIdAsync(long id, CancellationToken ct)
    {
        // Find all DishTag for a given DishId, including the related Tag
        return await _context.DishTags
            .Include(x => x.Tag)
                .ThenInclude(t => t.ValueNameText)
                    .ThenInclude(t => t.I18nTranslations)
            .Where(x => x.DishId == id)
            .ToListAsync(ct);
    }

    public async Task<List<uint>> GetTagIdsByDishIdAsync(long dishId, CancellationToken ct)
    {
        return await _context.DishTags
        .Where(x => x.DishId == dishId)
        .Select(x => x.TagId)
        .ToListAsync(ct);
    }

    public async Task RemoveDishTagsAsync(long dishId, List<uint> tagIds, CancellationToken ct)
    {
        await _context.DishTags
        .Where(x => x.DishId == dishId && tagIds.Contains(x.TagId))
        .ExecuteDeleteAsync(ct);
    }

    public async Task<List<Dish>> GetActiveDishesAsync()
    {
        return await _context.Dishes
            .AsNoTracking()
            .Include(d => d.DishStatusLv)
            .Include(d => d.DishMedia)
                .ThenInclude(dm => dm.Media)
            .Include(d => d.DishNameText)
                .ThenInclude(t => t.I18nTranslations)
            .Include(d => d.DescriptionText)
                .ThenInclude(t => t.I18nTranslations)
            .Include(d => d.SloganText)
                .ThenInclude(t => t.I18nTranslations)
            .Include(d => d.NoteText)
                .ThenInclude(t => t.I18nTranslations)
            .Include(d => d.ShortDescriptionText)
                .ThenInclude(t => t.I18nTranslations)
            .Where(d =>
                d.IsOnline == true &&
                d.DishStatusLv.ValueCode == DishStatusCode.AVAILABLE.ToString())
            .OrderBy(d => d.DisplayOrder)
            .ToListAsync();
    }

    public async Task<List<Dish>> GetByIdsAsync(List<long> ids, CancellationToken ct)
    {
        if (ids == null || ids.Count == 0)
            return new List<Dish>();

        return await _context.Dishes
            .AsNoTracking()
            .Where(d => ids.Contains(d.DishId))
            .ToListAsync(ct);
    }
   
}

