using Core.Interface.Service.Others;

namespace Infa.Others;

/// <summary>
/// No-operation cache implementation that does nothing.
/// Use in production when caching is not needed (simplest deployment).
/// All operations return default values immediately without storing anything.
/// </summary>
public sealed class NoCacheService : ICacheService
{
    public Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken ct = default)
    {
        // Do nothing - no caching
        return Task.CompletedTask;
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        // Always return default (cache miss)
        return Task.FromResult<T?>(default);
    }

    public Task<bool> RemoveAsync(string key, CancellationToken ct = default)
    {
        // Nothing to remove
        return Task.FromResult(true);
    }

    public Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        // Nothing exists
        return Task.FromResult(false);
    }

    public Task<long> ListRightPushAsync<T>(string key, T value, CancellationToken ct = default)
    {
        // Do nothing - return 0
        return Task.FromResult(0L);
    }

    public Task<T?> ListLeftPopAsync<T>(string key, CancellationToken ct = default)
    {
        // Always empty
        return Task.FromResult<T?>(default);
    }
}
