using Core.DTO.Email;
using Core.Interface.Service.Email;
using Microsoft.EntityFrameworkCore.Storage;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using IDatabase = StackExchange.Redis.IDatabase;

namespace Infa.Email
{
    public sealed class RedisEmailQueue : IEmailQueue
    {
        private readonly IDatabase _db;
        private readonly string _queueKey;

        public RedisEmailQueue(IConnectionMultiplexer mux, string queueKey = "email:queue")
        {
            _db = mux.GetDatabase();
            _queueKey = queueKey;
        }

        public Task EnqueueAsync(QueuedEmail email, CancellationToken ct = default)
            => _db.ListRightPushAsync(_queueKey, JsonSerializer.Serialize(email));

        public async Task<QueuedEmail> DequeueAsync(CancellationToken ct = default)
        {
            // Simple polling. Good enough for most apps; later you can move to Redis Streams for “blocking”.
            while (!ct.IsCancellationRequested)
            {
                var value = await _db.ListLeftPopAsync(_queueKey);
                if (value.HasValue)
                    return JsonSerializer.Deserialize<QueuedEmail>(value!)!;

                await Task.Delay(250, ct);
            }

            throw new OperationCanceledException(ct);
        }

    }
}
