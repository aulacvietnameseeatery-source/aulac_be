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
}
