using Core.DTO.Dish;
using Core.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interface.Repo;

/// <summary>
/// Repository interface for dish data access operations.
/// </summary>
public interface IDishRepository
{
    /// <summary>
    /// Finds a dish by its ID.
    /// </summary>
    /// <param name="dishId">The dish ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The dish entity or null if not found</returns>
    Task<Dish?> FindByIdAsync(long dishId, CancellationToken ct = default);

    /// <summary>
    /// Updates the status of a dish.
    /// </summary>
    /// <param name="dishId">The dish ID</param>
    /// <param name="statusLvId">The new status lookup value ID</param>
    /// <param name="ct">Cancellation token</param>
    Task UpdateStatusAsync(long dishId, uint statusLvId, CancellationToken ct = default);

    /// <summary>
    /// Checks if a dish exists.
    /// </summary>
    /// <param name="dishId">The dish ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if the dish exists; otherwise false</returns>
    Task<bool> ExistsAsync(long dishId, CancellationToken ct = default);
    Task<Dish?> GetDishByIdAsync(long dishId, CancellationToken cancellationToken = default);
    Task<(List<Dish> Items, int TotalCount)> GetDishesAsync(GetDishesRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Lấy danh sách tất cả các Category Name để hiển thị trong Filter Dropdown.
    /// </summary>
    Task<List<string>> GetAllCategoriesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy danh sách các Status của Dish từ bảng LookupValues để hiển thị trong Filter Dropdown.
    /// </summary>
    Task<List<LookupValue>> GetDishStatusesAsync(CancellationToken cancellationToken = default);

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
    Task<Dish?> FindByIdForActionAsync(long id, CancellationToken ct);

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
    Task<DishTag?> FindTagByDishIdAsync(long id, ushort typeId, CancellationToken ct);
}
