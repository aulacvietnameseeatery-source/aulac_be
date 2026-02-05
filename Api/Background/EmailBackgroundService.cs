using Core.DTO.Email;
using Core.Interface.Service.Email;
using Polly;
using Polly.Retry;
using StackExchange.Redis;

namespace Api.Background;

public sealed class EmailBackgroundService : BackgroundService
{
    private readonly IEmailQueue _queue;
    private readonly IDeadLetterSink _dlq;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EmailBackgroundService> _logger;

    public EmailBackgroundService(
        IEmailQueue queue,
        IDeadLetterSink dlq,
        IServiceScopeFactory scopeFactory,
        ILogger<EmailBackgroundService> logger)
    {
        _queue = queue;
        _dlq = dlq;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Email send retry (SMTP transient failures, etc.)
        const int sendRetries = 3;

        AsyncRetryPolicy sendRetryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                sendRetries,
                attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)), // 2s, 4s, 8s
                (ex, delay, attempt, _) =>
                    _logger.LogWarning(ex, "Email retry {Attempt}/{Total} after {Delay}", attempt, sendRetries, delay)
            );

        // When Redis is down, DequeueAsync will throw.
        // We must NOT let that escape ExecuteAsync, otherwise the Host will stop.
        var redisBackoffSeconds = 2;

        while (!stoppingToken.IsCancellationRequested)
        {
            QueuedEmail? job = null;

            try
            {
                // 1) Try to dequeue a job
                job = await _queue.DequeueAsync(stoppingToken);

                // Reset Redis backoff after a successful dequeue call
                redisBackoffSeconds = 2;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (RedisConnectionException ex)
            {
                _logger.LogWarning(ex,
                    "Redis unavailable for email queue. Retrying dequeue in {DelaySeconds}s...",
                    redisBackoffSeconds);

                await Task.Delay(TimeSpan.FromSeconds(redisBackoffSeconds), stoppingToken);
                redisBackoffSeconds = Math.Min(redisBackoffSeconds * 2, 30);
                continue;
            }
            catch (RedisTimeoutException ex)
            {
                _logger.LogWarning(ex,
                    "Redis timeout while dequeuing email. Retrying dequeue in {DelaySeconds}s...",
                    redisBackoffSeconds);

                await Task.Delay(TimeSpan.FromSeconds(redisBackoffSeconds), stoppingToken);
                redisBackoffSeconds = Math.Min(redisBackoffSeconds * 2, 30);
                continue;
            }
            catch (Exception ex)
            {
                // Any other unexpected dequeue errors should not stop the service
                _logger.LogError(ex,
                    "Unexpected error while dequeuing email. Retrying in {DelaySeconds}s...",
                    redisBackoffSeconds);

                await Task.Delay(TimeSpan.FromSeconds(redisBackoffSeconds), stoppingToken);
                redisBackoffSeconds = Math.Min(redisBackoffSeconds * 2, 30);
                continue;
            }

            // If your DequeueAsync returns null when queue is empty, prevent hot-loop
            if (job == null)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                continue;
            }

            // 2) Send email (scoped sender)
            using var scope = _scopeFactory.CreateScope();
            var sender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

            try
            {
                await sendRetryPolicy.ExecuteAsync(ct =>
                    sender.SendAsync(new EmailMessage(job.To, job.Subject, job.HtmlBody), ct),
                    stoppingToken
                );
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Email permanently failed to {To} (CorrelationId={CorrelationId})",
                    job.To, job.CorrelationId);

                // DLQ can also be Redis-backed, so protect it too
                try
                {
                    await _dlq.WriteAsync(
                        new DeadLetterEmail(
                            job,
                            ex.ToString(),
                            Attempt: 1 + sendRetries,
                            FailedAt: DateTimeOffset.UtcNow
                        ),
                        stoppingToken
                    );
                }
                catch (Exception dlqEx)
                {
                    // Last line of defense: never crash background service
                    _logger.LogError(dlqEx,
                        "Failed to write to DeadLetterSink for {To} (CorrelationId={CorrelationId}).",
                        job.To, job.CorrelationId);

                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                }
            }
        }
    }
}
