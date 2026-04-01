using Core.DTO.Email;
using Core.Interface.Service.Email;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace Infa.Service;

/// <summary>
/// Hangfire job runner for queued emails.
/// </summary>
public sealed class EmailJobRunner
{
    private readonly IEmailSender _emailSender;
    private readonly ILogger<EmailJobRunner> _logger;

    public EmailJobRunner(
        IEmailSender emailSender,
        ILogger<EmailJobRunner> logger)
    {
        _emailSender = emailSender;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task SendQueuedEmailAsync(QueuedEmail queuedEmail)
    {
        _logger.LogInformation(
            "Processing Hangfire email job for {To} (CorrelationId: {CorrelationId})",
            queuedEmail.To,
            queuedEmail.CorrelationId);

        await _emailSender.SendAsync(
            new EmailMessage(queuedEmail.To, queuedEmail.Subject, queuedEmail.HtmlBody),
            CancellationToken.None);

        _logger.LogInformation(
            "Sent email successfully to {To} (CorrelationId: {CorrelationId})",
            queuedEmail.To,
            queuedEmail.CorrelationId);
    }
}
