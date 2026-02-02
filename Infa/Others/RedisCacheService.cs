using Core.Interface.Service.Others;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Infa.Others
{
    public sealed class RedisCacheService : ICacheService
    {
        private readonly IDatabase _db;
        private static readonly JsonSerializerOptions JsonOpt = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public RedisCacheService(IConnectionMultiplexer mux)
            => _db = mux.GetDatabase();

        public async Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken ct = default)
        {
            var payload = JsonSerializer.Serialize(value, JsonOpt);
            await _db.StringSetAsync(key, payload, (Expiration)ttl);
        }

        public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
        {
            var val = await _db.StringGetAsync(key);
            if (!val.HasValue) return default;
            return JsonSerializer.Deserialize<T>(val!, JsonOpt);
        }

        public Task<bool> RemoveAsync(string key, CancellationToken ct = default)
            => _db.KeyDeleteAsync(key);

        public Task<bool> ExistsAsync(string key, CancellationToken ct = default)
            => _db.KeyExistsAsync(key);

        public Task<long> ListRightPushAsync<T>(string key, T value, CancellationToken ct = default)
        {
            var payload = JsonSerializer.Serialize(value, JsonOpt);
            return _db.ListRightPushAsync(key, payload);
        }

        public async Task<T?> ListLeftPopAsync<T>(string key, CancellationToken ct = default)
        {
            var val = await _db.ListLeftPopAsync(key);
            if (!val.HasValue) return default;
            return JsonSerializer.Deserialize<T>(val!, JsonOpt);
        }
    }

}
