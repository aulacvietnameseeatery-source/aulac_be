using Core.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interface.Repo
{
    public interface IMediaRepository
    {
        Task<MediaAsset> AddMediaAsync(MediaAsset media, CancellationToken ct);
        Task AddDishMediaAsync(DishMedium dishMedia, CancellationToken ct);

        Task AddAsync(MediaAsset asset, CancellationToken ct);
    }
}
