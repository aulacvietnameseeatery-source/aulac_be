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

namespace Infa.Repo
{
    public class DishRepository : IDishRepository
    {
        private readonly RestaurantMgmtContext _db;

        public DishRepository(RestaurantMgmtContext db)
        {
            _db = db; // Store the injected DbContext for database operations
        }

        public async Task AddAsync(Dish dish, CancellationToken ct)
        {
            _db.Dishes.Add(dish); // Add new Dish entity to the context
            await _db.SaveChangesAsync(ct); // Persist changes to the database
        }

        public async Task<Dish?> FindByIdAsync(long id, CancellationToken ct)
        {
            // Retrieve a Dish by ID, including related entities for full details
            return await _db.Dishes
            .Include(x => x.DishNameText).ThenInclude(x => x.I18nTranslations)
            .Include(x => x.DescriptionText).ThenInclude(x => x.I18nTranslations)
            .Include(x => x.ShortDescriptionText).ThenInclude(x => x.I18nTranslations)
            .Include(x => x.SloganText).ThenInclude(x => x.I18nTranslations)
            .Include(x => x.NoteText).ThenInclude(x => x.I18nTranslations)
            .Include(x => x.DishStatusLv)
            .Include(x => x.Category)
            .Include(x => x.DishMedia)
                .ThenInclude(x => x.Media)
                    .ThenInclude(x => x.MediaTypeLv)
            .FirstOrDefaultAsync(x => x.DishId == id, ct); // Return the first match or null
        }

        public async Task<List<LookupValue>> GetActiveDishStatusEntitiesAsync()
        {
            // Get all active dish status lookup values
            return await _db.LookupValues
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
            return await _db.DishCategories
                .AsNoTracking()
                .OrderBy(c => c.CategoryId)
                .ToListAsync();
        }

        public async Task<List<LookupValue>> GetAllActiveTagsAsync()
        {
            // Get all active tag lookup values
            return await _db.LookupValues
                .AsNoTracking()
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
            _db.DishTags.Add(dishTag); // Add new DishTag entity to the context
            await _db.SaveChangesAsync(ct); // Persist changes to the database
        }

        public async Task<DishTag?> FindTagByDishIdAsync(long id, CancellationToken ct)
        {
            // Find the DishTag for a given DishId, including the related Tag
            return await _db.DishTags
                .Include(x => x.Tag)
            .FirstOrDefaultAsync(x => x.DishId == id, ct);
        }
    }
}
