using Core.DTO.Dish;
using Core.Entity;
using Core.Interface.Repo;
using Infa.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

    public async Task<(List<Dish> Items, int TotalCount)> GetDishesAsync(GetDishesRequest request, CancellationToken cancellationToken = default)
    {
        var dish = _context.Dishes
            .Include(d => d.Category)
            .Include(d => d.DishStatusLv)
            .Include(d => d.DishMedia) 
            .AsNoTracking()
            .AsQueryable();

        if (request.IsCustomerView)
        {
            //khách chỉ lấy món ăn với status available (42)
            dish = dish.Where(d => d.IsOnline == true &&
                                     d.DishStatusLvId == (long)DishStatusCode.AVAILABLE);
        }
        else
        {
            if (request.Status.HasValue)
            {
                dish = dish.Where(d => d.DishStatusLvId == (long)request.Status.Value);
            }
        }

        // Sorting logic 
        if (string.IsNullOrWhiteSpace(request.SortBy))
        {
            dish = dish.OrderByDescending(d => d.DishId); 
        }
        else
        {
            switch (request.SortBy.ToLower())
            {
                case "price":
                    dish = request.IsDescending ? dish.OrderByDescending(d => d.Price) : dish.OrderBy(d => d.Price);
                    break;
                case "name":
                    dish = request.IsDescending ? dish.OrderByDescending(d => d.DishName) : dish.OrderBy(d => d.DishName);
                    break;
                default:
                    dish = dish.OrderByDescending(d => d.CreatedAt);
                    break;
            }
        }

        var totalCount = await dish.CountAsync(cancellationToken);

        var items = await dish
            .Skip((request.PageIndex - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<Dish?> GetDishByIdAsync(long dishId, CancellationToken cancellationToken = default)
    {
        return await _context.Dishes
            .Include(d => d.Category)
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
}

