using Core.Interface.Repo;
using Core.Interface.Service.Entity;
using Core.Interface.Service.Others;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infa.Service;

/// <summary>
/// SINGLETON implementation of ILookupResolver using Redis cache and scoped database loader.
/// Provides fast, distributed cached lookups for (type_id, value_code) → value_id resolution.
/// Does NOT inject DbContext directly - uses IServiceScopeFactory to create scopes for database access.
/// </summary>
public class LookupResolver : ILookupResolver
{
    private readonly ICacheService _cache;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LookupResolver> _logger;
    private readonly SemaphoreSlim _warmupLock = new(1, 1);

    private const string CacheKey = "lookup_resolver:all";
    private const string LastUpdatedKey = "lookup_resolver:last_updated";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromHours(24);

    public LookupResolver(
        ICacheService cache,
        IServiceScopeFactory scopeFactory,
        ILogger<LookupResolver> logger)
    {
        _cache = cache;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<uint> GetIdAsync(ushort typeId, string valueCode, CancellationToken ct = default)
    {
        var id = await TryGetIdAsync(typeId, valueCode, ct);

        if (id == null)
        {
            throw new KeyNotFoundException(
       $"Lookup value not found for type_id={typeId}, value_code='{valueCode}'");
        }

        return id.Value;
    }

    /// <inheritdoc />
    public async Task<uint> GetIdAsync(ushort typeId, System.Enum valueCodeEnum, CancellationToken ct = default)
    {
        if (valueCodeEnum == null)
        {
            throw new ArgumentNullException(nameof(valueCodeEnum));
        }

        return await GetIdAsync(typeId, valueCodeEnum.ToString(), ct);
    }

    /// <inheritdoc />
    public async Task<uint?> TryGetIdAsync(ushort typeId, string valueCode, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(valueCode))
        {
            return null;
        }

        // Normalize the value code (case-insensitive, trimmed)
        var normalizedCode = valueCode.Trim().ToUpperInvariant();
        var key = BuildCacheKey(typeId, normalizedCode);

        // Get the lookup dictionary from cache
        var lookupDict = await GetOrLoadCacheAsync(ct);

        return lookupDict.TryGetValue(key, out var valueId) ? valueId : null;
    }

    /// <inheritdoc />
    public async Task<uint?> TryGetIdAsync(ushort typeId, System.Enum valueCodeEnum, CancellationToken ct = default)
    {
        if (valueCodeEnum == null)
        {
            return null;
        }

        return await TryGetIdAsync(typeId, valueCodeEnum.ToString(), ct);
    }

    /// <inheritdoc />
    public async Task WarmUpAsync(CancellationToken cancellationToken = default)
    {
        // Use semaphore to prevent concurrent warmup operations
        await _warmupLock.WaitAsync(cancellationToken);
        try
        {
            _logger.LogInformation("Warming up lookup resolver cache...");

            var lookupDict = await LoadFromDatabaseAsync(cancellationToken);

            // Store in distributed cache (Redis) with expiration
            await _cache.SetAsync(CacheKey, lookupDict, CacheExpiration, cancellationToken);

            // Store the last updated timestamp
            await UpdateLastUpdatedTimestampAsync(cancellationToken);

            _logger.LogInformation(
                 "Lookup resolver cache warmed up with {Count} entries",
            lookupDict.Count);
        }
        finally
        {
            _warmupLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<bool> RefreshIfChangedAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if the lookup_value table has an UpdatedAt column
            var hasUpdatedAt = await HasUpdatedAtColumnAsync(cancellationToken);

            if (!hasUpdatedAt)
            {
                // No UpdatedAt column - refresh is a no-op
                _logger.LogDebug("Lookup refresh skipped: UpdatedAt column does not exist in lookup_value table");
                return false;
            }

            // Get the maximum UpdatedAt timestamp from the database
            var maxUpdatedAt = await GetMaxUpdatedAtAsync(cancellationToken);

            if (maxUpdatedAt == null)
            {
                _logger.LogWarning("No active lookup values found in database");
                return false;
            }

            // Get the cached last updated timestamp
            var cachedLastUpdated = await _cache.GetAsync<DateTime?>(LastUpdatedKey, cancellationToken);

            // If cache is empty or data has changed, reload
            if (cachedLastUpdated == null || maxUpdatedAt > cachedLastUpdated)
            {
                _logger.LogInformation(
          "Lookup data changed (cached: {Cached}, current: {Current}). Refreshing cache...",
         cachedLastUpdated,
              maxUpdatedAt);

                await WarmUpAsync(cancellationToken);
                return true;
            }

            _logger.LogDebug("Lookup data unchanged. Cache refresh skipped.");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during lookup cache refresh check");
            return false;
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// Gets the lookup dictionary from cache or triggers warmup if cache is empty.
    /// </summary>
    private async Task<Dictionary<string, uint>> GetOrLoadCacheAsync(CancellationToken ct = default)
    {
        // Try to get from distributed cache (Redis)
        var cachedDict = await _cache.GetAsync<Dictionary<string, uint>>(CacheKey, ct);

        if (cachedDict != null)
        {
            return cachedDict;
        }

        // Cache miss - trigger warmup
        _logger.LogWarning(
                "Lookup cache miss. Loading from database. Consider calling WarmUpAsync() at startup.");

        await WarmUpAsync(ct);

        // Read from cache again after warmup
        cachedDict = await _cache.GetAsync<Dictionary<string, uint>>(CacheKey, ct);

        if (cachedDict == null)
        {
            _logger.LogError("Failed to load lookup cache even after warmup");
            return new Dictionary<string, uint>(StringComparer.Ordinal);
        }

        return cachedDict;
    }

    /// <summary>
    /// Loads all active lookup values from the database using a scoped ILookupLoader.
    /// </summary>
    private async Task<Dictionary<string, uint>> LoadFromDatabaseAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var loader = scope.ServiceProvider.GetRequiredService<ILookupRepo>();
        return await loader.LoadAllAsync(ct);
    }

    /// <summary>
    /// Gets the maximum UpdatedAt timestamp using a scoped ILookupLoader.
    /// </summary>
    private async Task<DateTime?> GetMaxUpdatedAtAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var loader = scope.ServiceProvider.GetRequiredService<ILookupRepo>();
        return await loader.GetMaxUpdatedAtAsync(ct);
    }

    /// <summary>
    /// Checks if UpdatedAt column exists using a scoped ILookupLoader.
    /// </summary>
    private async Task<bool> HasUpdatedAtColumnAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var loader = scope.ServiceProvider.GetRequiredService<ILookupRepo>();
        return await loader.HasUpdatedAtColumnAsync(ct);
    }

    /// <summary>
    /// Updates the last updated timestamp in cache.
    /// </summary>
    private async Task UpdateLastUpdatedTimestampAsync(CancellationToken cancellationToken)
    {
        try
        {
            var hasUpdatedAt = await HasUpdatedAtColumnAsync(cancellationToken);

            if (!hasUpdatedAt)
            {
                return; // No UpdatedAt column, skip
            }

            var maxUpdatedAt = await GetMaxUpdatedAtAsync(cancellationToken);

            if (maxUpdatedAt != null)
            {
                await _cache.SetAsync(LastUpdatedKey, maxUpdatedAt.Value, CacheExpiration, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update last updated timestamp");
        }
    }

    /// <summary>
    /// Builds a cache key from type_id and normalized value_code.
    /// </summary>
    private static string BuildCacheKey(ushort typeId, string normalizedValueCode)
    {
        return $"{typeId}|{normalizedValueCode}";
    }

    #endregion
}

/*
 * USAGE EXAMPLES:
 * 
 * // 1. Basic string lookup (async)
 * var valueId = await _lookupResolver.GetIdAsync(1, "ACTIVE");
 * 
 * // 2. Enum lookup (async)
 * public enum OrderStatus { PENDING, CONFIRMED, COMPLETED }
 * var statusId = await _lookupResolver.GetIdAsync(1, OrderStatus.PENDING);
 * 
 * // 3. Safe lookup with null handling (async)
 * var paymentMethodId = await _lookupResolver.TryGetIdAsync(2, "CREDIT_CARD");
 * if (paymentMethodId == null)
 * {
 *     // Handle missing lookup value
 * }
 * 
 * // 4. Application startup - warm up cache
 * await _lookupResolver.WarmUpAsync();
 * 
 * // 5. Background job - refresh if changed
 * var wasRefreshed = await _lookupResolver.RefreshIfChangedAsync();
 * if (wasRefreshed)
 * {
 *     _logger.LogInformation("Lookup cache was refreshed");
 * }
 * 
 * // 6. Using extension methods (see LookupResolverExtensions)
 * var statusId = await OrderStatus.PENDING.IdAsync(_lookupResolver, typeId: 1);
 * 
 * ARCHITECTURE NOTES:
 * 
 * - LookupResolver is SINGLETON (no DbContext injection)
 * - Uses IServiceScopeFactory to create scopes for database access
 * - All database operations delegated to scoped ILookupLoader
 * - Cache is shared across multiple application instances (Redis)
 * - SemaphoreSlim prevents concurrent warmup operations
 * - 24-hour TTL (Time To Live) configured
 * - Cache keys: "lookup_resolver:all" and "lookup_resolver:last_updated"
 */
