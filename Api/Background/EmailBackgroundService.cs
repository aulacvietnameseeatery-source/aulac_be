using Core.DTO.Email;
using Core.Interface.Service.Email;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;

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
        const int retries = 3;

        var retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retries,
                attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                (ex, delay, attempt, _) =>
                    _logger.LogWarning(ex, "Email retry {Attempt}/{Total} after {Delay}", attempt, retries, delay)
            );

        while (!stoppingToken.IsCancellationRequested)
        {
            QueuedEmail job;
            try
            {
                job = await _queue.DequeueAsync(stoppingToken);
            }
            catch (OperationCanceledException) { break; }

            using var scope = _scopeFactory.CreateScope();
            var sender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

            try
            {
                await retryPolicy.ExecuteAsync(ct =>
                    sender.SendAsync(new EmailMessage(job.To, job.Subject, job.HtmlBody), ct),
                    stoppingToken
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email permanently failed to {To} (CorrelationId={CorrelationId})",
                    job.To, job.CorrelationId);

                await _dlq.WriteAsync(
                    new DeadLetterEmail(job, ex.ToString(), Attempt: 1 + retries, FailedAt: DateTimeOffset.UtcNow),
                    stoppingToken
                );
            }
        }
    }
}
