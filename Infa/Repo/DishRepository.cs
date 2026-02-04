using Core.Entity;
using Core.Interface.Repo;
using Infa.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infa.Repo
{
    public class DishRepository : IDishRepository
    {
        private readonly RestaurantMgmtContext _db;

        public DishRepository(RestaurantMgmtContext db)
        {
            _db = db;
        }

        public async Task AddAsync(Dish dish, CancellationToken ct)
        {
            _db.Dishes.Add(dish);
            await _db.SaveChangesAsync(ct);
        }

        public Task<Dish?> FindByIdAsync(long id, CancellationToken ct)
        {
            return _db.Dishes.FirstOrDefaultAsync(d => d.DishId == id, ct);
        }
    }
}
