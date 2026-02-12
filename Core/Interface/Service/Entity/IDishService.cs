using Core.DTO.Dish;
using Core.Entity;

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
    /// <param name="langCode">Language code. Supported: "en" (English - default), "fr" (French), "vi" (Vietnamese)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dish detail DTO or null if not found</returns>
    Task<DishDetailDto?> GetDishByIdAsync(long id, string? langCode = null, CancellationToken cancellationToken = default);

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
    // Trả về List DTO và TotalCount
    Task<(List<DishManagementDto> Items, int TotalCount)> GetDishesForAdminAsync(GetDishesRequest request, CancellationToken cancellationToken = default);

    Task<(List<DishDisplayDto> Items, int TotalCount)> GetDishesForCustomerAsync(GetDishesRequest request, CancellationToken cancellationToken = default);
}

