using Core.DTO.Email;
using Core.Interface.Service.Email;
using Core.Interface.Service.Others;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Infa.Email
{
    public sealed class RedisDeadLetterSink : IDeadLetterSink
    {
        private readonly ICacheService _cache;
        private readonly string _dlqKey;

        public RedisDeadLetterSink(ICacheService cache, string dlqKey = "email:dlq")
        {
            _cache = cache;
            _dlqKey = dlqKey;
        }

        public Task WriteAsync(DeadLetterEmail item, CancellationToken ct = default)
            => _cache.ListRightPushAsync(_dlqKey, item, ct);
    }


}
