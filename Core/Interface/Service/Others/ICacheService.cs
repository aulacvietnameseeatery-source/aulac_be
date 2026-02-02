using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interface.Service.Others
{
    public interface ICacheService
    {
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
