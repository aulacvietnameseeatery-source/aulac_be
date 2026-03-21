namespace Core.DTO.Notification;

/// <summary>
/// DTO returned for a single notification preference entry.
/// </summary>
public class NotificationPreferenceDto
{
    public string NotificationType { get; set; } = null!;
    public bool IsEnabled { get; set; }
    public bool SoundEnabled { get; set; }
}

/// <summary>
/// Request to update multiple notification preferences at once.
/// </summary>
public class UpdateNotificationPreferencesRequest
{
    public List<NotificationPreferenceItemRequest> Preferences { get; set; } = new();
}

public class NotificationPreferenceItemRequest
{
    public string NotificationType { get; set; } = null!;
    public bool IsEnabled { get; set; }
    public bool SoundEnabled { get; set; }
}
