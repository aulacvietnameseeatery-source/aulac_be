using Core.DTO.Email;
using Core.Interface.Service.Email;
using Core.Interface.Service.Others;

namespace Infa.Email
{
    /// <summary>
    /// Cache-based dead letter sink implementation.
    /// Works with any ICacheService implementation (Redis, In-Memory, etc.)
    /// </summary>
    public sealed class CacheDeadLetterSink : IDeadLetterSink
    {
        private readonly ICacheService _cache;
        private readonly string _dlqKey;

        public CacheDeadLetterSink(ICacheService cache, string dlqKey = "email:dlq")
        {
            _cache = cache;
            _dlqKey = dlqKey;
        }

        public Task WriteAsync(DeadLetterEmail item, CancellationToken ct = default)
                => _cache.ListRightPushAsync(_dlqKey, item, ct);
    }
}
