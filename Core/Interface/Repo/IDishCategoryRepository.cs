using Core.DTO.DishCategory;
using Core.DTO.General;
using Core.Entity;

namespace Core.Interface.Repo;

/// <summary>
/// Repository interface for dish category data access
/// </summary>
public interface IDishCategoryRepository
{


    /// <summary>
    /// Get paginated dish categories with filtering
    /// </summary>
    /// <param name="query">Query parameters including pagination and filters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated result of dish categories</returns>
    Task<PagedResultDTO<DishCategoryDto>> GetAllCategoriesAsync(DishCategoryListQueryDTO query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get dish category by ID
    /// </summary>
    /// <param name="id">Category ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dish category or null if not found</returns>
    Task<DishCategory?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new dish category
    /// </summary>
    /// <param name="category">Category to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created category</returns>
    Task<DishCategory> CreateAsync(DishCategory category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing dish category
    /// </summary>
    /// <param name="category">Category to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated category</returns>
    Task<DishCategory> UpdateAsync(DishCategory category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if category has dishes
    /// </summary>
    /// <param name="id">Category ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if category has dishes</returns>
    Task<bool> HasDishesAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a category name already exists (case-insensitive)
    /// </summary>
    /// <param name="name">Category name to check</param>
    /// <param name="excludeId">Category ID to exclude from check (for update scenario)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if name exists</returns>
    Task<bool> ExistsByNameAsync(string name, long? excludeId = null, CancellationToken cancellationToken = default);
}
