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
        /// <summary>
        /// Adds a new MediaAsset asynchronously to the repository.
        /// </summary>
        /// <param name="media">The MediaAsset to add.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The added MediaAsset.</returns>
        Task<MediaAsset> AddMediaAsync(MediaAsset media, CancellationToken ct);

        /// <summary>
        /// Associates a DishMedium entity with a dish asynchronously.
        /// </summary>
        /// <param name="dishMedia">The DishMedium to add.</param>
        /// <param name="ct">Cancellation token.</param>
        Task AddDishMediaAsync(DishMedium dishMedia, CancellationToken ct);

        /// <summary>
        /// Adds a MediaAsset asynchronously to the repository.
        /// </summary>
        /// <param name="asset">The MediaAsset to add.</param>
        /// <param name="ct">Cancellation token.</param>
        Task AddAsync(MediaAsset asset, CancellationToken ct);

        /// <summary>
        /// Retrieves a list of DishMedium entities by dish ID and a collection of media IDs asynchronously.
        /// </summary>
        /// <param name="dishId">The ID of the dish.</param>
        /// <param name="mediaIds">A collection of media IDs to filter by.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>List of matching DishMedium entities.</returns>
        Task<List<DishMedium>> GetDishMediaByIdsAsync(
            long dishId,
            IReadOnlyCollection<long> mediaIds,
            CancellationToken ct
        );

        /// <summary>
        /// Removes the association of a DishMedium entity from a dish asynchronously.
        /// </summary>
        /// <param name="dishMedia">The DishMedium to remove.</param>
        /// <param name="ct">Cancellation token.</param>
        Task RemoveDishMediaAsync(DishMedium dishMedia, CancellationToken ct);

        /// <summary>
        /// Removes a MediaAsset asynchronously from the repository.
        /// </summary>
        /// <param name="media">The MediaAsset to remove.</param>
        /// <param name="ct">Cancellation token.</param>
        Task RemoveMediaAsync(MediaAsset media, CancellationToken ct);
    }
}
