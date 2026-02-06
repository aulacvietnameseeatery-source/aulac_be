using Core.Entity;
using Core.Interface.Repo;
using Infa.Data;
using Microsoft.EntityFrameworkCore;

namespace Infa.Repo;

public class DishRepository : IDishRepository
{
    private readonly RestaurantMgmtContext _context;

    public DishRepository(RestaurantMgmtContext context)
    {
        _context = context;
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
}
