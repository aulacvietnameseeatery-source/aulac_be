using Core.DTO.Email;
using Core.Interface.Service.Email;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infa.Email;

/// <summary>
/// Synchronous email queue - sends emails immediately without queuing.
/// Use in production when background processing is not needed.
/// </summary>
public sealed class DirectEmailQueue : IEmailQueue
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DirectEmailQueue> _logger;

    public DirectEmailQueue(
        IServiceScopeFactory scopeFactory,
        ILogger<DirectEmailQueue> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task EnqueueAsync(QueuedEmail email, CancellationToken ct = default)
    {
        // Send email directly (synchronously) instead of queuing
        using var scope = _scopeFactory.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

        try
        {
            _logger.LogInformation("Sending email directly to {To} (no queue - CacheMode: None)", email.To);

            await sender.SendAsync(new EmailMessage(email.To, email.Subject, email.HtmlBody), ct);

            _logger.LogInformation("Email sent successfully to {To} (CorrelationId: {CorrelationId})", email.To, email.CorrelationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To} (CorrelationId: {CorrelationId})", email.To, email.CorrelationId);

            // Rethrow to notify caller
            throw;
        }
    }

    public Task<QueuedEmail> DequeueAsync(CancellationToken ct = default)
    {
        // This is never called when using DirectEmailQueue
        // because EmailBackgroundService is not needed
        throw new NotSupportedException("DequeueAsync is not supported in DirectEmailQueue. Emails are sent synchronously via EnqueueAsync.");
    }
}
