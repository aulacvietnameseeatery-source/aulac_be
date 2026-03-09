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
    /// Gets all tables that are available for reservation (manual).
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
    /// Finds a table by ID with all detail includes (Status, Type, Zone, Media, Orders, Reservations, ServiceErrors).
    /// </summary>
    /// <param name="tableId">The table ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The table entity with details or null if not found</returns>
    Task<RestaurantTable?> GetByIdWithDetailsAsync(long tableId, CancellationToken ct = default);

    /// <summary>
    /// Checks if a table exists.
    /// </summary>
    /// <param name="tableId">The table ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if the table exists; otherwise false</returns>
    Task<bool> ExistsAsync(long tableId, CancellationToken ct = default);

    /// <summary>
    /// Checks if a table code already exists, optionally excluding a specific table.
    /// </summary>
    /// <param name="code">The table code</param>
    /// <param name="excludeId">Optional ID of the table to exclude from check</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if the table code exists; otherwise false</returns>
    Task<bool> TableCodeExistsAsync(string code, long? excludeId = null, CancellationToken ct = default);

    /// <summary>
    /// Counts active orders (PENDING, IN_PROGRESS) for a table.
    /// </summary>
    /// <param name="tableId">The table ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The count of active orders</returns>
    Task<int> CountActiveOrdersAsync(long tableId, CancellationToken ct = default);

    /// <summary>
    /// Counts upcoming reservations (PENDING, CONFIRMED) for a table.
    /// </summary>
    /// <param name="tableId">The table ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The count of upcoming reservations</returns>
    Task<int> CountUpcomingReservationsAsync(long tableId, CancellationToken ct = default);

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

    Task<List<RestaurantTable>> GetTablesWithRelationsAsync(CancellationToken ct = default);
    void Add(RestaurantTable table);
    Task UpdateAsync(RestaurantTable table, CancellationToken ct);

    /// <summary>
    /// Validates that a lookup value_id exists, is active, and belongs to the specified type_id.
    /// </summary>
    Task<bool> IsValidLookupAsync(uint valueId, ushort typeId, CancellationToken ct = default);

    /// <summary>
    /// Gets the ValueCode for a given lookup value_id.
    /// Returns null if not found or inactive.
    /// </summary>
    Task<string?> GetLookupValueCodeAsync(uint valueId, CancellationToken ct = default);

    /// <summary>
    /// Bulk-sets the IsOnline flag for all non-deleted tables in a specific zone.
    /// Returns the number of affected rows.
    /// </summary>
    Task<int> BulkSetOnlineByZoneAsync(uint zoneLvId, bool isOnline, CancellationToken ct = default);

    /// <summary>
    /// Gets a TableMedium join record by table ID and media ID.
    /// </summary>
    Task<TableMedium?> GetTableMediaAsync(long tableId, long mediaId, CancellationToken ct = default);

    /// <summary>
    /// Adds a TableMedium join record to the context (does not save).
    /// </summary>
    void AddTableMedia(TableMedium tableMedium);

    /// <summary>
    /// Removes a TableMedium join record from the context (does not save).
    /// </summary>
    void RemoveTableMedia(TableMedium tableMedium);

    /// <summary>
    /// Sets the QR image foreign key on a table row (does not save — caller must SaveChanges).
    /// </summary>
    Task SetQrImageAsync(long tableId, long mediaId, CancellationToken ct = default);

    Task<bool> TryOccupyIfAvailableAsync(
        long tableId,
        uint availableLvId,
        uint occupiedLvId,
        CancellationToken ct);
}
