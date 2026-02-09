using Core.Entity;

namespace Core.Interface.Repo;

/// <summary>
/// Repository interface for restaurant table data access operations.
/// </summary>
public interface ITableRepository
{
    /// <summary>
    /// Gets all tables that are available for reservation.
    /// Filters out tables under maintenance (LOCKED status).
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of available tables</returns>
    Task<List<RestaurantTable>> GetAvailableTablesAsync(CancellationToken ct = default);

    /// <summary>
    /// Finds a table by its ID.
    /// </summary>
    /// <param name="tableId">The table ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The table entity or null if not found</returns>
    Task<RestaurantTable?> GetByIdAsync(long tableId, CancellationToken ct = default);

    /// <summary>
    /// Checks if a table exists.
    /// </summary>
    /// <param name="tableId">The table ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if the table exists; otherwise false</returns>
    Task<bool> ExistsAsync(long tableId, CancellationToken ct = default);
}
