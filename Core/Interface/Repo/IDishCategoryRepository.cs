using Core.Entity;

namespace Core.Interface.Repo;

/// <summary>
/// Repository interface for dish category data access
/// </summary>
public interface IDishCategoryRepository
{
    /// <summary>
    /// Get all dish categories
    /// </summary>
    /// <param name="includeDisabled">Whether to include disabled categories</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of dish categories</returns>
    Task<List<DishCategory>> GetAllAsync(bool includeDisabled = false, CancellationToken cancellationToken = default);

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
    /// Delete a dish category
    /// </summary>
    /// <param name="id">Category ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default);

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
