using Core.Interface.Service.Entity;
using System.Text.Json;

namespace Core.Extensions;

/// <summary>
/// Helper to read event-based email notification settings from the SystemSetting table.
/// Convention: notification.{eventCode}.enabled  (BOOL)
///             notification.{eventCode}.recipients (JSON string array; legacy STRING is also supported)
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

        // Preferred format: JSON array in ValueType=JSON
        var jsonRecipients = await settings.GetJsonAsync<List<string>>(
            $"notification.{eventCode}.recipients", null, ct);

        if (jsonRecipients is not null)
        {
            var normalized = jsonRecipients
                .Select(e => (e ?? string.Empty).Trim().ToLowerInvariant())
                .Where(IsValidEmail)
                .Distinct()
                .ToList();

            return (true, normalized);
        }

        // Backward compatibility: old STRING format (semicolon/newline separated)
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

        // If a JSON array is stored in a STRING field, still support it.
        if (LooksLikeJsonArray(raw))
        {
            try
            {
                var arr = JsonSerializer.Deserialize<List<string>>(raw);
                if (arr is not null)
                {
                    return arr
                        .Select(e => (e ?? string.Empty).Trim().ToLowerInvariant())
                        .Where(IsValidEmail)
                        .Distinct()
                        .ToList();
                }
            }
            catch
            {
                // Ignore parse errors and fall through to delimiter parsing.
            }
        }

        return raw
            .Split([';', '\n', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(e => e.Trim().ToLowerInvariant())
            .Where(IsValidEmail)
            .Distinct()
            .ToList();
    }

    private static bool LooksLikeJsonArray(string raw)
    {
        var trimmed = raw.Trim();
        return trimmed.StartsWith("[") && trimmed.EndsWith("]");
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
