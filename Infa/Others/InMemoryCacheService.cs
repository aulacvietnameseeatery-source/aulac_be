using Core.Interface.Service.Others;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace Infa.Others;

/// <summary>
/// In-memory cache implementation using IMemoryCache.
/// Suitable for single-server deployments (production without Redis).
/// </summary>
public sealed class InMemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private static readonly JsonSerializerOptions JsonOpt = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public InMemoryCacheService(IMemoryCache cache)
        => _cache = cache;

    public Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken ct = default)
    {
        var options = new MemoryCacheEntryOptions();
        if (ttl.HasValue)
        {
            options.SetAbsoluteExpiration(ttl.Value);
        }

        _cache.Set(key, value, options);
        return Task.CompletedTask;
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        _cache.TryGetValue<T>(key, out var value);
        return Task.FromResult(value);
    }

    public Task<bool> RemoveAsync(string key, CancellationToken ct = default)
    {
        _cache.Remove(key);
        return Task.FromResult(true);
    }

    public Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        var exists = _cache.TryGetValue(key, out _);
        return Task.FromResult(exists);
    }

    public Task<long> ListRightPushAsync<T>(string key, T value, CancellationToken ct = default)
    {
        // Get or create list
        var list = _cache.GetOrCreate(key, entry => new List<string>())!;

        var payload = JsonSerializer.Serialize(value, JsonOpt);
        list.Add(payload);

        return Task.FromResult((long)list.Count);
    }

    public Task<T?> ListLeftPopAsync<T>(string key, CancellationToken ct = default)
    {
        if (!_cache.TryGetValue<List<string>>(key, out var list) || list == null || list.Count == 0)
        {
            return Task.FromResult<T?>(default);
        }

        var payload = list[0];
        list.RemoveAt(0);

        var item = JsonSerializer.Deserialize<T>(payload, JsonOpt);
        return Task.FromResult(item);
    }
}
