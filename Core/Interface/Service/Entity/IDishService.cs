using Core.DTO.Dish;

namespace Core.Interface.Service.Entity;

/// <summary>
/// Service interface for dish business logic operations.
/// </summary>
public interface IDishService
{
    /// <summary>
    /// Updates the status of a dish.
    /// </summary>
    /// <param name="dishId">The dish ID</param>
    /// <param name="newStatus">The new status code</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated dish status information</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the dish is not found</exception>
    Task<DishStatusDto> UpdateDishStatusAsync(
        long dishId,
        DishStatusCode newStatus,
        CancellationToken ct = default);
}
