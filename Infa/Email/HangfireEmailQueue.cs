using Core.DTO.Email;
using Core.Interface.Service.Email;
using Hangfire;
using Infa.Service;
using Microsoft.Extensions.Logging;

namespace Infa.Email;

/// <summary>
/// Hangfire-backed email queue implementation.
/// Enqueue schedules a background job instead of writing to cache.
/// </summary>
public sealed class HangfireEmailQueue : IEmailQueue
{
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<HangfireEmailQueue> _logger;

    public HangfireEmailQueue(
        IBackgroundJobClient backgroundJobClient,
        ILogger<HangfireEmailQueue> logger)
    {
        _backgroundJobClient = backgroundJobClient;
        _logger = logger;
    }

    public Task EnqueueAsync(QueuedEmail email, CancellationToken ct = default)
    {
        var jobId = _backgroundJobClient.Enqueue<EmailJobRunner>(
            runner => runner.SendQueuedEmailAsync(email));

        _logger.LogInformation(
            "Enqueued Hangfire email job {JobId} for {To} (CorrelationId: {CorrelationId})",
            jobId,
            email.To,
            email.CorrelationId);

        return Task.CompletedTask;
    }

    public Task<QueuedEmail> DequeueAsync(CancellationToken ct = default)
    {
        throw new NotSupportedException(
            "DequeueAsync is not supported in HangfireEmailQueue. Jobs are processed by Hangfire workers.");
    }
}
