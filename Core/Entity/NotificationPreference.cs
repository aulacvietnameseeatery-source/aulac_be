namespace Core.Entity;

public class NotificationPreference
{
    public long NotificationPreferenceId { get; set; }

    public long UserId { get; set; }

    /// <summary>
    /// The notification type code (e.g. "NEW_ORDER", "LOW_STOCK_ALERT").
    /// </summary>
    public string NotificationType { get; set; } = null!;

    /// <summary>
    /// Whether in-app + real-time push is enabled for this type.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Whether sound should play on the client for this type.
    /// </summary>
    public bool SoundEnabled { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
