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
    public class MediaRepository : IMediaRepository
    {
        private readonly RestaurantMgmtContext _db;

        public MediaRepository(RestaurantMgmtContext db)
        {
            _db = db; // Store the injected DbContext for database operations
        }

        public async Task<MediaAsset> AddMediaAsync(MediaAsset media, CancellationToken ct)
        {
            _db.MediaAssets.Add(media); // Add new media asset to the context
            await _db.SaveChangesAsync(ct); // Persist changes to the database
            return media; // Return the added media asset
        }

        public async Task AddDishMediaAsync(DishMedium dishMedia, CancellationToken ct)
        {
            _db.DishMedia.Add(dishMedia); // Add new dish-media association
            await _db.SaveChangesAsync(ct); // Save changes to the database
        }

        public async Task AddAsync(MediaAsset asset, CancellationToken ct)
        {
            _db.MediaAssets.Add(asset); // Add media asset to the context
            await _db.SaveChangesAsync(ct); // Save changes to the database
        }

        public async Task<List<DishMedium>> GetDishMediaByIdsAsync(long dishId, IReadOnlyCollection<long> mediaIds, CancellationToken ct)
        {
            return await _db.DishMedia
                .Include(x => x.Media) // Eager load related MediaAsset
                .Where(x =>
                    x.DishId == dishId && // Filter by dish ID
                    mediaIds.Contains(x.MediaId) // Filter by provided media IDs
                )
                .ToListAsync(ct); // Execute query and return results as a list
        }

        public Task RemoveDishMediaAsync(DishMedium dishMedia, CancellationToken ct)
        {
            _db.DishMedia.Remove(dishMedia); // Remove dish-media association from context
            return Task.CompletedTask; // No async DB operation here, just mark for removal
        }

        public Task RemoveMediaAsync(MediaAsset media, CancellationToken ct)
        {
            _db.MediaAssets.Remove(media); // Remove media asset from context
            return Task.CompletedTask; // No async DB operation here, just mark for removal
        }
    }

}
