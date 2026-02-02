namespace Core.Interface.Service.Entity;

/// <summary>
/// Service for resolving lookup value codes to their database IDs.
/// Provides cached, case-insensitive lookup resolution with support for enums.
/// </summary>
public interface ILookupResolver
{
    /// <summary>
    /// Gets the value_id for a given type_id and value_code.
    /// </summary>
    /// <param name="typeId">The lookup type ID</param>
    /// <param name="valueCode">The value code (case-insensitive)</param>
    /// <returns>The value_id</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the lookup value is not found</exception>
    Task<uint> GetIdAsync(ushort typeId, string valueCode, CancellationToken ct = default);


    /// <summary>
    /// Gets the value_id for a given type_id and enum value_code.
    /// </summary>
    /// <param name="typeId">The lookup type ID</param>
    /// <param name="valueCodeEnum">The enum representing the value code</param>
    /// <returns>The value_id</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the lookup value is not found</exception>
    Task<uint> GetIdAsync(ushort typeId, System.Enum valueCodeEnum, CancellationToken ct = default);


    /// <summary>
    /// Attempts to get the value_id for a given type_id and value_code.
    /// </summary>
    /// <param name="typeId">The lookup type ID</param>
    /// <param name="valueCode">The value code (case-insensitive)</param>
    /// <returns>The value_id if found; otherwise null</returns>
    Task<uint?> TryGetIdAsync(ushort typeId, string valueCode, CancellationToken ct = default);


    /// <summary>
    /// Attempts to get the value_id for a given type_id and enum value_code.
    /// </summary>
    /// <param name="typeId">The lookup type ID</param>
    /// <param name="valueCodeEnum">The enum representing the value code</param>
    /// <returns>The value_id if found; otherwise null</returns>
    Task<uint?> TryGetIdAsync(ushort typeId, System.Enum valueCodeEnum, CancellationToken ct = default);


    /// <summary>
    /// Preloads all active lookup values into the cache.
    /// Should be called during application startup.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task WarmUpAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes the cache if the lookup data has changed.
    /// Uses the MAX(updated_at) timestamp to detect changes.
    /// If updated_at column does not exist, this operation is a no-op.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if cache was refreshed; false if no changes detected or updated_at is not available</returns>
    Task<bool> RefreshIfChangedAsync(CancellationToken cancellationToken = default);
}
