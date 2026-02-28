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
    /// Gets all tables that are available for reservation.
    /// Filters out tables under maintenance (LOCKED status).
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of available tables</returns>
    Task<List<RestaurantTable>> GetManualAvailableTablesAsync(CancellationToken ct = default);

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

    /// <summary>
    /// Gets tables with dynamic filters for POS/Table Management screen.
    /// </summary>
    Task<(List<RestaurantTable> Items, int TotalCount)> GetTablesForManagementAsync(DTO.Table.GetTableManagementRequest request, CancellationToken ct = default);

    /// <summary>Finds a table by its code (e.g. "TB-002"). Returns null if not found.</summary>
    Task<RestaurantTable?> GetByCodeAsync(string tableCode, CancellationToken ct = default);

    /// <summary>
    /// Updates the status of a table by setting its status lookup value ID.
    /// </summary>
    /// <param name="tableId">The table ID</param>
    /// <param name="statusLvId">The new status lookup value ID</param>
    /// <param name="ct">Cancellation token</param>
    Task UpdateStatusAsync(long tableId, uint statusLvId, CancellationToken ct = default);
}
