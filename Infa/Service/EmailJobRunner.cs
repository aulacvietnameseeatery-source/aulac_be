using Core.DTO.Email;
using Core.Interface.Service;
using Core.Interface.Service.Email;
using Core.Interface.Service.Entity;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace Infa.Service;

/// <summary>
/// Hangfire job runner for queued emails.
/// </summary>
public sealed class EmailJobRunner
{
    private readonly IEmailSender _emailSender;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly ISystemSettingService _systemSettingService;
    private readonly ILogger<EmailJobRunner> _logger;

    private const string SettingStoreEmail = "store.email";

    public EmailJobRunner(
        IEmailSender emailSender,
        IEmailTemplateService emailTemplateService,
        ISystemSettingService systemSettingService,
        ILogger<EmailJobRunner> logger)
    {
        _emailSender = emailSender;
        _emailTemplateService = emailTemplateService;
        _systemSettingService = systemSettingService;
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

    /// <summary>
    /// Hangfire job: loads the confirmation template, builds the body, and delivers
    /// the customer reservation confirmation email entirely off the request thread.
    /// </summary>
    [AutomaticRetry(Attempts = 3)]
    public async Task SendReservationCustomerEmailAsync(
        long reservationId,
        string toEmail,
        string customerName,
        DateTime reservedTime,
        int partySize,
        string tableCodes)
    {
        _logger.LogInformation(
            "Hangfire: sending customer confirmation for reservation {ReservationId} to {Email}",
            reservationId, toEmail);

        const string templateCode = "RESERVATION_CONFIRM";
        var template = await _emailTemplateService.GetByCodeAsync(templateCode);
        if (template == null)
        {
            _logger.LogWarning("Email template {TemplateCode} not found. Skipping.", templateCode);
            return;
        }

        var body = template.BodyHtml
            .Replace("{{CustomerName}}", customerName)
            .Replace("{{ReservedTime}}", reservedTime.ToString("dd/MM/yyyy HH:mm"))
            .Replace("{{PartySize}}", partySize.ToString())
            .Replace("{{TableCode}}", tableCodes)
            .Replace("{{TableCodes}}", tableCodes)
            .Replace("{{ReservationId}}", reservationId.ToString());

        await _emailSender.SendAsync(
            new EmailMessage(toEmail, template.Subject, body),
            CancellationToken.None);

        _logger.LogInformation(
            "Hangfire: customer confirmation sent for reservation {ReservationId}", reservationId);
    }

    /// <summary>
    /// Hangfire job: loads the admin notification template, resolves the store email,
    /// builds the body, and delivers the admin reservation notification email off the request thread.
    /// </summary>
    [AutomaticRetry(Attempts = 3)]
    public async Task SendReservationAdminEmailAsync(
        long reservationId,
        string customerName,
        DateTime reservedTime,
        int partySize,
        string tableCodes)
    {
        _logger.LogInformation(
            "Hangfire: sending admin notification for reservation {ReservationId}", reservationId);

        const string templateCode = "RESERVATION_CONFIRM_ADMIN";
        var adminEmail = await _systemSettingService.GetStringAsync(SettingStoreEmail);
        if (string.IsNullOrWhiteSpace(adminEmail))
        {
            _logger.LogWarning("Store email setting not configured. Skipping admin email for reservation {ReservationId}.", reservationId);
            return;
        }

        var template = await _emailTemplateService.GetByCodeAsync(templateCode);
        if (template == null)
        {
            _logger.LogWarning("Email template {TemplateCode} not found. Skipping.", templateCode);
            return;
        }

        var body = template.BodyHtml
            .Replace("{{CustomerName}}", customerName)
            .Replace("{{ReservedTime}}", reservedTime.ToString("dd/MM/yyyy HH:mm"))
            .Replace("{{PartySize}}", partySize.ToString())
            .Replace("{{TableCode}}", tableCodes)
            .Replace("{{TableCodes}}", tableCodes)
            .Replace("{{ReservationId}}", reservationId.ToString());

        await _emailSender.SendAsync(
            new EmailMessage(adminEmail, template.Subject, body),
            CancellationToken.None);

        _logger.LogInformation(
            "Hangfire: admin notification sent for reservation {ReservationId}", reservationId);
    }
}
