using Core.DTO.Email;
using Core.Interface.Service.Email;
using Core.Interface.Service.Others;

namespace Infa.Email
{
    /// <summary>
    /// Cache-based email queue implementation.
    /// Works with any ICacheService implementation (Redis, In-Memory, etc.)
    /// </summary>
    public sealed class CacheEmailQueue : IEmailQueue
    {
        private readonly ICacheService _cache;
        private readonly string _queueKey;

        public CacheEmailQueue(ICacheService cache, string queueKey = "email:queue")
        {
            _cache = cache;
            _queueKey = queueKey;
        }

        public Task EnqueueAsync(QueuedEmail email, CancellationToken ct = default)
            => _cache.ListRightPushAsync(_queueKey, email, ct);

        public async Task<QueuedEmail> DequeueAsync(CancellationToken ct = default)
        {
            // simple polling (fine for now). Upgrade later to Redis Streams if needed.
            while (!ct.IsCancellationRequested)
            {
                var item = await _cache.ListLeftPopAsync<QueuedEmail>(_queueKey, ct);
                if (item is not null) return item;

                await Task.Delay(250, ct);
            }

            throw new OperationCanceledException(ct);
        }
    }
}
