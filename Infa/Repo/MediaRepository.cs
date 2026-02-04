using Core.Entity;
using Core.Interface.Repo;
using Infa.Data;
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
            _db = db;
        }

        public async Task<MediaAsset> AddMediaAsync(MediaAsset media, CancellationToken ct)
        {
            _db.MediaAssets.Add(media);
            await _db.SaveChangesAsync(ct);
            return media;
        }

        public async Task AddDishMediaAsync(DishMedium dishMedia, CancellationToken ct)
        {
            _db.DishMedia.Add(dishMedia);
            await _db.SaveChangesAsync(ct);
        }

        public async Task AddAsync(MediaAsset asset, CancellationToken ct)
        {
            _db.MediaAssets.Add(asset);
            await _db.SaveChangesAsync(ct);
        }
    }

}
