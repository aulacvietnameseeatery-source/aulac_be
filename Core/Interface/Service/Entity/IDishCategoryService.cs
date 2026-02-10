using Core.DTO.DishCategory;

namespace Core.Interface.Service.Entity;

/// <summary>
/// Service interface for dish category-related business logic
/// </summary>
public interface IDishCategoryService
{
    /// <summary>
    /// Get all dish categories
    /// </summary>
    /// <param name="includeDisabled">Whether to include disabled categories</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of dish categories</returns>
    Task<List<DishCategoryDto>> GetAllCategoriesAsync(bool includeDisabled = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get dish category by ID
    /// </summary>
    /// <param name="id">Category ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dish category DTO or null if not found</returns>
    Task<DishCategoryDto?> GetCategoryByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new dish category
    /// </summary>
    /// <param name="request">Create category request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created dish category</returns>
    Task<DishCategoryDto> CreateCategoryAsync(CreateDishCategoryRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing dish category
    /// </summary>
    /// <param name="id">Category ID</param>
    /// <param name="request">Update category request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated dish category</returns>
    /// <exception cref="KeyNotFoundException">Thrown when category not found</exception>
    Task<DishCategoryDto> UpdateCategoryAsync(long id, UpdateDishCategoryRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Toggle category status (enable/disable)
    /// </summary>
    /// <param name="id">Category ID</param>
    /// <param name="isDisabled">New disabled status</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated dish category</returns>
    /// <exception cref="KeyNotFoundException">Thrown when category not found</exception>
    Task<DishCategoryDto> ToggleCategoryStatusAsync(long id, bool isDisabled, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a dish category
    /// </summary>
    /// <param name="id">Category ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted successfully</returns>
    /// <exception cref="KeyNotFoundException">Thrown when category not found</exception>
    /// <exception cref="InvalidOperationException">Thrown when category has dishes</exception>
    Task<bool> DeleteCategoryAsync(long id, CancellationToken cancellationToken = default);
}
