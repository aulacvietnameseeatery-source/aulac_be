using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interface.Service.Others
{
    public interface ICacheService
    {
        /// <summary>
        /// Indicates whether this cache implementation actually stores data.
        /// Returns <c>false</c> for no-op implementations (e.g. <c>NoCacheService</c>),
        /// allowing callers to fall back to direct database access instead of
        /// relying on cache read-back after a write.
        /// </summary>
        bool IsAvailable { get; }

        // key-value (string/json)
        Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken ct = default);
        Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
        Task<bool> RemoveAsync(string key, CancellationToken ct = default);
        Task<bool> ExistsAsync(string key, CancellationToken ct = default);

        // list operations (queue)
        Task<long> ListRightPushAsync<T>(string key, T value, CancellationToken ct = default);
        Task<T?> ListLeftPopAsync<T>(string key, CancellationToken ct = default);
    }

}
