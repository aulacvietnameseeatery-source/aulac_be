using Core.DTO.Email;
using Core.DTO.EmailTemplate;
using Core.Extensions;
using Core.Interface.Service;
using Core.Interface.Service.Email;
using Core.Interface.Service.Entity;
using Hangfire;
using Infa.Email;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace Infa.Service;

/// <summary>
/// Hangfire job runner for queued emails.
/// Uses <see cref="IMemoryCache"/> to avoid repeated DB template lookups,
/// and <see cref="RestaurantOptions"/> for timezone instead of hardcode.
/// </summary>
public sealed class EmailJobRunner
{
    private readonly IEmailSender _emailSender;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly ISystemSettingService _systemSettingService;
    private readonly ILogger<EmailJobRunner> _logger;
    private readonly TimeZoneInfo _restaurantTz;
    private readonly IMemoryCache _cache;
    private readonly int _maxParallelSends;

    private const string SettingStoreEmail = "store.email";
    private static readonly TimeSpan TemplateCacheDuration = TimeSpan.FromMinutes(30);

    public EmailJobRunner(
        IEmailSender emailSender,
        IEmailTemplateService emailTemplateService,
        ISystemSettingService systemSettingService,
        IOptions<RestaurantOptions> restaurantOptions,
        IOptions<SmtpOptions> smtpOptions,
        IMemoryCache cache,
        ILogger<EmailJobRunner> logger)
    {
        _emailSender = emailSender;
        _emailTemplateService = emailTemplateService;
        _systemSettingService = systemSettingService;
        _restaurantTz = restaurantOptions.Value.TimeZone;
        _maxParallelSends = Math.Max(1, smtpOptions.Value.MaxParallelSends);
        _cache = cache;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task SendQueuedEmailAsync(QueuedEmail queuedEmail)
    {
        var sw = Stopwatch.StartNew();
        long lastMs = 0;
        string currentStep = "job_start";

        void LogMilestone(string step)
        {
            var elapsedMs = sw.ElapsedMilliseconds;
            var stepMs = elapsedMs - lastMs;
            lastMs = elapsedMs;
            currentStep = step;

            _logger.LogInformation(
                "[EMAIL-TIMELINE] Flow=queued-email Step={Step} TimestampUtc={TimestampUtc:o} ElapsedMs={ElapsedMs} StepMs={StepMs} To={To} CorrelationId={CorrelationId}",
                step,
                DateTimeOffset.UtcNow,
                elapsedMs,
                stepMs,
                queuedEmail.To,
                queuedEmail.CorrelationId);
        }

        _logger.LogInformation(
            "[EMAIL-METRIC] Processing queued email for {To} (CorrelationId: {CorrelationId})",
            queuedEmail.To,
            queuedEmail.CorrelationId);
        LogMilestone("job_started");

        try
        {
            LogMilestone("smtp_send_start");
            await _emailSender.SendAsync(
                new EmailMessage(queuedEmail.To, queuedEmail.Subject, queuedEmail.HtmlBody),
                CancellationToken.None);
            LogMilestone("smtp_send_done");

            _logger.LogInformation(
                "[EMAIL-METRIC] Sent queued email to {To} in {Ms}ms (CorrelationId: {CorrelationId})",
                queuedEmail.To, sw.ElapsedMilliseconds, queuedEmail.CorrelationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[EMAIL-METRIC] Failed queued email | To={To} | Subject={Subject} | CorrelationId={CorrelationId} | DurationMs={DurationMs}",
                queuedEmail.To,
                queuedEmail.Subject,
                queuedEmail.CorrelationId,
                sw.ElapsedMilliseconds);

            _logger.LogError(
                "[EMAIL-TIMELINE] Flow=queued-email Step={Step} TimestampUtc={TimestampUtc:o} ElapsedMs={ElapsedMs} To={To} CorrelationId={CorrelationId}",
                currentStep,
                DateTimeOffset.UtcNow,
                sw.ElapsedMilliseconds,
                queuedEmail.To,
                queuedEmail.CorrelationId);
            throw;
        }
        finally
        {
            LogMilestone("job_end");
            _logger.LogInformation(
                "[EMAIL-METRIC] End queued email job | To={To} | CorrelationId={CorrelationId} | TotalDurationMs={DurationMs}",
                queuedEmail.To,
                queuedEmail.CorrelationId,
                sw.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// Hangfire job: send reservation customer + admin notifications concurrently.
    /// </summary>
    [AutomaticRetry(Attempts = 3)]
    public async Task SendReservationBothEmailsAsync(
        long reservationId,
        string? customerEmail,
        string customerName,
        DateTime reservedTime,
        int partySize,
        string tableCodes)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogInformation(
            "[EMAIL-METRIC] Combined email job started | ReservationId={ReservationId} | CustomerEmail={CustomerEmail}",
            reservationId,
            customerEmail);

        var tasks = new List<Task>
        {
            SendReservationAdminEmailAsync(reservationId, customerName, reservedTime, partySize, tableCodes)
        };

        if (!string.IsNullOrWhiteSpace(customerEmail))
        {
            tasks.Add(
                SendReservationCustomerEmailAsync(
                    reservationId,
                    customerEmail,
                    customerName,
                    reservedTime,
                    partySize,
                    tableCodes));
        }

        await Task.WhenAll(tasks);

        _logger.LogInformation(
            "[EMAIL-METRIC] Combined email job completed | ReservationId={ReservationId} | ParallelTasks={TaskCount} | TotalDurationMs={DurationMs}",
            reservationId,
            tasks.Count,
            sw.ElapsedMilliseconds);
    }

    /// <summary>
    /// Hangfire job: customer reservation confirmation email.
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
        var sw = Stopwatch.StartNew();
        long lastMs = 0;
        string currentStep = "job_start";

        void LogMilestone(string step)
        {
            var elapsedMs = sw.ElapsedMilliseconds;
            var stepMs = elapsedMs - lastMs;
            lastMs = elapsedMs;
            currentStep = step;

            _logger.LogInformation(
                "[EMAIL-TIMELINE] Flow=reservation-customer Step={Step} TimestampUtc={TimestampUtc:o} ElapsedMs={ElapsedMs} StepMs={StepMs} ReservationId={ReservationId} To={To}",
                step,
                DateTimeOffset.UtcNow,
                elapsedMs,
                stepMs,
                reservationId,
                toEmail);
        }

        _logger.LogInformation(
            "[EMAIL-METRIC] Job started: customer confirmation for reservation {ReservationId} to {Email}",
            reservationId, toEmail);
        LogMilestone("job_started");

        try
        {
            const string templateCode = "RESERVATION_CONFIRM";
            LogMilestone("template_load_start");
            var template = await GetCachedTemplateAsync(templateCode);
            LogMilestone("template_load_done");
            _logger.LogInformation("[EMAIL-METRIC] Template loaded in {Ms}ms", sw.ElapsedMilliseconds);

            if (template == null)
            {
                _logger.LogWarning("Email template {TemplateCode} not found. Skipping.", templateCode);
                return;
            }

            var body = BuildReservationEmailBody(template.BodyHtml,
                customerName, reservedTime, partySize, tableCodes, reservationId);
            LogMilestone("body_build_done");

            LogMilestone("smtp_send_start");
            await _emailSender.SendAsync(
                new EmailMessage(toEmail, template.Subject, body),
                CancellationToken.None);
            LogMilestone("smtp_send_done");

            _logger.LogInformation(
                "[EMAIL-METRIC] Customer confirmation sent for reservation {ReservationId} in {Ms}ms total",
                reservationId, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[EMAIL-METRIC] Failed customer confirmation email | ReservationId={ReservationId} | To={To} | CustomerName={CustomerName} | DurationMs={DurationMs}",
                reservationId,
                toEmail,
                customerName,
                sw.ElapsedMilliseconds);

            _logger.LogError(
                "[EMAIL-TIMELINE] Flow=reservation-customer Step={Step} TimestampUtc={TimestampUtc:o} ElapsedMs={ElapsedMs} ReservationId={ReservationId} To={To}",
                currentStep,
                DateTimeOffset.UtcNow,
                sw.ElapsedMilliseconds,
                reservationId,
                toEmail);
            throw;
        }
        finally
        {
            LogMilestone("job_end");
            _logger.LogInformation(
                "[EMAIL-METRIC] End customer confirmation job | ReservationId={ReservationId} | To={To} | TotalDurationMs={DurationMs}",
                reservationId,
                toEmail,
                sw.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// Hangfire job: admin/notification-recipient reservation notification email.
    /// Reads recipients from notification.reservation_created.recipients setting;
    /// falls back to store.email when notification settings are not configured.
    /// </summary>
    [AutomaticRetry(Attempts = 3)]
    public async Task SendReservationAdminEmailAsync(
        long reservationId,
        string customerName,
        DateTime reservedTime,
        int partySize,
        string tableCodes)
    {
        var sw = Stopwatch.StartNew();
        long lastMs = 0;
        string currentStep = "job_start";

        void LogMilestone(string step)
        {
            var elapsedMs = sw.ElapsedMilliseconds;
            var stepMs = elapsedMs - lastMs;
            lastMs = elapsedMs;
            currentStep = step;

            _logger.LogInformation(
                "[EMAIL-TIMELINE] Flow=reservation-admin Step={Step} TimestampUtc={TimestampUtc:o} ElapsedMs={ElapsedMs} StepMs={StepMs} ReservationId={ReservationId}",
                step,
                DateTimeOffset.UtcNow,
                elapsedMs,
                stepMs,
                reservationId);
        }

        _logger.LogInformation(
            "[EMAIL-METRIC] Job started: admin notification for reservation {ReservationId}",
            reservationId);
        LogMilestone("job_started");

        try
        {
            // 1. Resolve recipients from notification settings
            LogMilestone("recipient_resolve_start");
            var (isEnabled, recipients) = await NotificationSettingHelper
                .GetEventNotificationAsync(_systemSettingService, "reservation_created");
            LogMilestone("recipient_resolve_done");

            if (!isEnabled || recipients.Count == 0)
            {
                // Fallback to store.email
                var storeEmail = await _systemSettingService.GetStringAsync(SettingStoreEmail);
                if (string.IsNullOrWhiteSpace(storeEmail))
                {
                    _logger.LogWarning(
                        "No notification recipients configured and store.email is empty. Skipping admin email for reservation {ReservationId}.",
                        reservationId);
                    return;
                }
                recipients = new List<string> { storeEmail };
            }

            const string templateCode = "RESERVATION_CONFIRM_ADMIN";
            LogMilestone("template_load_start");
            var template = await GetCachedTemplateAsync(templateCode);
            LogMilestone("template_load_done");
            _logger.LogInformation("[EMAIL-METRIC] Template loaded in {Ms}ms", sw.ElapsedMilliseconds);

            if (template == null)
            {
                _logger.LogWarning("Email template {TemplateCode} not found. Skipping.", templateCode);
                return;
            }

            var body = BuildReservationEmailBody(template.BodyHtml,
                customerName, reservedTime, partySize, tableCodes, reservationId);
            LogMilestone("body_build_done");

            // Send to ALL recipients in parallel
            LogMilestone("smtp_send_start");
            _logger.LogInformation(
                "[EMAIL-METRIC] Admin fan-out sending with max parallel {MaxParallelSends} for {RecipientCount} recipients",
                _maxParallelSends,
                recipients.Count);

            await SendEmailsWithConcurrencyLimitAsync(recipients, template.Subject, body, _maxParallelSends, CancellationToken.None);
            LogMilestone("smtp_send_done");

            _logger.LogInformation(
                "[EMAIL-METRIC] Admin notification sent for reservation {ReservationId} to {Count} recipients ({Emails}) in {Ms}ms total",
                reservationId, recipients.Count, string.Join(", ", recipients), sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[EMAIL-METRIC] Failed admin notification email | ReservationId={ReservationId} | CustomerName={CustomerName} | DurationMs={DurationMs}",
                reservationId,
                customerName,
                sw.ElapsedMilliseconds);

            _logger.LogError(
                "[EMAIL-TIMELINE] Flow=reservation-admin Step={Step} TimestampUtc={TimestampUtc:o} ElapsedMs={ElapsedMs} ReservationId={ReservationId}",
                currentStep,
                DateTimeOffset.UtcNow,
                sw.ElapsedMilliseconds,
                reservationId);
            throw;
        }
        finally
        {
            LogMilestone("job_end");
            _logger.LogInformation(
                "[EMAIL-METRIC] End admin notification job | ReservationId={ReservationId} | TotalDurationMs={DurationMs}",
                reservationId,
                sw.ElapsedMilliseconds);
        }
    }

    // ── private helpers ───────────────────────────────────────────────

    private string BuildReservationEmailBody(
        string bodyHtml,
        string customerName,
        DateTime reservedTimeUtc,
        int partySize,
        string tableCodes,
        long reservationId)
    {
        var localTime = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(reservedTimeUtc, DateTimeKind.Utc), _restaurantTz);

        return bodyHtml
            .Replace("{{CustomerName}}", customerName)
            .Replace("{{ReservedTime}}", localTime.ToString("dd/MM/yyyy HH:mm"))
            .Replace("{{PartySize}}", partySize.ToString())
            .Replace("{{TableCode}}", tableCodes)
            .Replace("{{TableCodes}}", tableCodes)
            .Replace("{{ReservationId}}", reservationId.ToString());
    }

    private async Task<EmailTemplateDto?> GetCachedTemplateAsync(string templateCode)
    {
        var cacheKey = $"email_template:{templateCode}";

        if (_cache.TryGetValue(cacheKey, out EmailTemplateDto? cached))
            return cached;

        var template = await _emailTemplateService.GetByCodeAsync(templateCode);

        if (template is not null)
        {
            _cache.Set(cacheKey, template, TemplateCacheDuration);
        }

        return template;
    }

    private async Task SendEmailsWithConcurrencyLimitAsync(
        IReadOnlyList<string> recipients,
        string subject,
        string body,
        int maxParallel,
        CancellationToken ct)
    {
        using var semaphore = new SemaphoreSlim(Math.Max(1, maxParallel));

        var tasks = recipients.Select(async recipient =>
        {
            await semaphore.WaitAsync(ct);
            try
            {
                await _emailSender.SendAsync(new EmailMessage(recipient, subject, body), ct);
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
    }
}
