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
}
