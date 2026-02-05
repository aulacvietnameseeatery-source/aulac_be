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
            .FirstOrDefaultAsync(d => d.DishId == dishId, cancellationToken);
    }
}
