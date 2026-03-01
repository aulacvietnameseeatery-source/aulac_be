using Core.Entity;

namespace Core.Interface.Repo;

/// <summary>
/// Service for loading lookup data from database.
/// This is a SCOPED service that has access to DbContext.
/// </summary>
public interface ILookupRepo
{
    /// <summary>
    /// Loads all active lookup values from database.
    /// Returns a dictionary with key format: "{typeId}|{NORMALIZED_VALUE_CODE}"
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Dictionary mapping composite keys to value_id</returns>
    Task<Dictionary<string, uint>> LoadAllAsync(CancellationToken ct = default);

    /// <summary>
  /// Gets the maximum UpdatedAt timestamp from active lookup values.
    /// Used for cache invalidation detection.
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Maximum UpdatedAt timestamp, or null if no values exist</returns>
  Task<DateTime?> GetMaxUpdatedAtAsync(CancellationToken ct = default);

    /// <summary>
    /// Checks if the lookup_value table has an UpdatedAt column.
    /// </summary>
 /// <param name="ct">Cancellation token</param>
    /// <returns>True if UpdatedAt column exists; otherwise false</returns>
    Task<bool> HasUpdatedAtColumnAsync(CancellationToken ct = default);

    Task<List<LookupValue>> GetAllActiveByTypeAsync(
        ushort typeId,
        CancellationToken ct
    );

    /// <summary>
    /// Gets a single lookup value by its ID. Returns null if not found or deleted.
    /// </summary>
    Task<LookupValue?> GetByIdAsync(uint valueId, CancellationToken ct = default);

    /// <summary>
    /// Checks if a lookup value with the given name already exists for the specified type.
    /// </summary>
  Task<bool> ValueNameExistsAsync(ushort typeId, string valueName, uint? excludeId = null, CancellationToken ct = default);

    /// <summary>
    /// Checks if a lookup value with the given code already exists for the specified type.
    /// </summary>
    Task<bool> ValueCodeExistsAsync(ushort typeId, string valueCode, uint? excludeId = null, CancellationToken ct = default);

    /// <summary>
    /// Gets the maximum sort order for a given lookup type. Returns 0 if none exist.
    /// </summary>
    Task<short> GetMaxSortOrderAsync(ushort typeId, CancellationToken ct = default);

    /// <summary>
    /// Adds a new lookup value entity to the context (does not save).
    /// </summary>
    void Add(LookupValue entity);

    /// <summary>
    /// Counts the number of restaurant tables that reference a given lookup value ID
 /// in any of the table's lookup foreign keys (zone, type, status).
    /// </summary>
    Task<int> CountTablesUsingLookupValueAsync(uint valueId, CancellationToken ct = default);
}
