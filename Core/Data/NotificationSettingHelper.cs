using Core.Interface.Service.Entity;

namespace Core.Helpers;

/// <summary>
/// Helper to read event-based email notification settings from the SystemSetting table.
/// Convention: notification.{eventCode}.enabled  (BOOL)
///             notification.{eventCode}.recipients (STRING, semicolon-separated emails)
/// </summary>
public static class NotificationSettingHelper
{
    /// <summary>
    /// Returns whether the event is enabled and the validated list of recipient emails.
    /// </summary>
    public static async Task<(bool IsEnabled, List<string> Recipients)> GetEventNotificationAsync(
        ISystemSettingService settings,
        string eventCode,
        CancellationToken ct = default)
    {
        var enabled = await settings.GetBoolAsync(
            $"notification.{eventCode}.enabled", false, ct);

        if (enabled != true)
            return (false, new List<string>());

        var raw = await settings.GetStringAsync(
            $"notification.{eventCode}.recipients", "", ct);

        var recipients = ParseAndValidateEmails(raw);
        return (true, recipients);
    }

    /// <summary>
    /// Splits a semicolon-separated email string, trims, dedupes, and validates each address.
    /// </summary>
    public static List<string> ParseAndValidateEmails(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return new List<string>();

        return raw
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(e => e.Trim().ToLowerInvariant())
            .Where(IsValidEmail)
            .Distinct()
            .ToList();
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return string.Equals(addr.Address, email, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
