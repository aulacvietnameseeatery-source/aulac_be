using Core.DTO.Dish;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interface.Service.Dish
{
    public interface IDishService
    {
        /// <summary>
        /// Creates a new dish with the specified details and associated images.
        /// </summary>
        /// <param name="request">The dish creation request data.</param>
        /// <param name="staticImages">A list of static image files for the dish.</param>
        /// <param name="images360">A list of 360-degree image files for the dish.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The ID of the newly created dish.</returns>
        Task<long> CreateDishAsync(
            CreateDishRequest request,
            IReadOnlyList<IFormFile> staticImages,
            IReadOnlyList<IFormFile> images360,
            CancellationToken ct
        );

        /// <summary>
        /// Retrieves the details of a dish by its ID.
        /// </summary>
        /// <param name="dishId">The ID of the dish to retrieve.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The detailed information of the dish.</returns>
        Task<DishDetailDto> GetDishByIdAsync(
            long dishId,
            CancellationToken cancellationToken
        );

        /// <summary>
        /// Updates an existing dish with new details and manages associated images.
        /// </summary>
        /// <param name="request">The dish update request data.</param>
        /// <param name="staticImages">A list of new static image files for the dish.</param>
        /// <param name="images360">A list of new 360-degree image files for the dish.</param>
        /// <param name="removedMediaIds">A list of media IDs to be removed from the dish.</param>
        /// <param name="ct">Cancellation token.</param>
        Task UpdateDishAsync(
            UpdateDishRequest request,
            IReadOnlyList<IFormFile> staticImages,
            IReadOnlyList<IFormFile> images360,
            IReadOnlyList<long> removedMediaIds,
            CancellationToken ct
        );

        /// <summary>
        /// Gets a list of all active dish statuses.
        /// </summary>
        /// <returns>A list of active dish status DTOs.</returns>
        Task<List<ActiveDishStatusDto>> GetActiveDishStatusesAsync();

        /// <summary>
        /// Gets a list of all dish categories.
        /// </summary>
        /// <returns>A list of simple dish category DTOs.</returns>
        Task<List<DishCategorySimpleDto>> GetAllDishCategoriesAsync();

        /// <summary>
        /// Gets a list of all active dish tags.
        /// </summary>
        /// <returns>A list of active dish tag DTOs.</returns>
        Task<List<DishTagDto>> GetAllActiveTagsAsync();
    }
}
