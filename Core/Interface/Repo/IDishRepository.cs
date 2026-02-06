using Core.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interface.Repo
{
    public interface IDishRepository
    {
        /// <summary>
        /// Asynchronously adds a new Dish entity to the repository.
        /// </summary>
        /// <param name="dish">The Dish entity to add.</param>
        /// <param name="ct">Cancellation token.</param>
        Task AddAsync(Dish dish, CancellationToken ct);

        /// <summary>
        /// Asynchronously finds a Dish by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the Dish.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The Dish entity if found; otherwise, null.</returns>
        Task<Dish?> FindByIdAsync(long id, CancellationToken ct);

        /// <summary>
        /// Retrieves a list of active dish status lookup values.
        /// </summary>
        /// <returns>List of active LookupValue entities representing dish statuses.</returns>
        Task<List<LookupValue>> GetActiveDishStatusEntitiesAsync();

        /// <summary>
        /// Retrieves all dish categories.
        /// </summary>
        /// <returns>List of all DishCategory entities.</returns>
        Task<List<DishCategory>> GetAllDishCategoriesAsync();

        /// <summary>
        /// Retrieves all active tag lookup values.
        /// </summary>
        /// <returns>List of active LookupValue entities representing tags.</returns>
        Task<List<LookupValue>> GetAllActiveTagsAsync();

        /// <summary>
        /// Asynchronously adds a new DishTag entity to the repository.
        /// </summary>
        /// <param name="dishTag">The DishTag entity to add.</param>
        /// <param name="ct">Cancellation token.</param>
        Task AddDishTagAsync(DishTag dishTag, CancellationToken ct);

        /// <summary>
        /// Asynchronously finds a DishTag by the associated Dish's unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the Dish.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The DishTag entity if found; otherwise, null.</returns>
        Task<DishTag?> FindTagByDishIdAsync(long id, CancellationToken ct);
    }
}
