using Core.DTO.Email;
using Core.Interface.Service.Email;
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
        private readonly IDatabase _db;
        private readonly string _dlqKey;

        public RedisDeadLetterSink(IConnectionMultiplexer mux, string dlqKey = "email:dlq")
        {
            _db = mux.GetDatabase();
            _dlqKey = dlqKey;
        }

        public Task WriteAsync(DeadLetterEmail item, CancellationToken ct = default)
            => _db.ListRightPushAsync(_dlqKey, JsonSerializer.Serialize(item));
    }

}
