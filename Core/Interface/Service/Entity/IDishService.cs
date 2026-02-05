using Core.DTO.Dish;

namespace Core.Interface.Service.Entity;

/// <summary>
/// Service interface for dish-related business logic
/// </summary>
public interface IDishService
{
    /// <summary>
    /// Get dish detail by ID
    /// </summary>
    /// <param name="id">Dish ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dish detail DTO or null if not found</returns>
    Task<DishDetailDto?> GetDishByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all dishes
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of dish detail DTOs</returns>
    Task<List<DishDetailDto>> GetAllDishesAsync(CancellationToken cancellationToken = default);
}
