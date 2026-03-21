using Core.Enum;

namespace Core.DTO.Notification;

/// <summary>
/// Internal request used by business modules to publish a notification.
/// </summary>
public class PublishNotificationRequest
{
    public string Type { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string? Body { get; set; }
    public string Priority { get; set; } = nameof(NotificationPriority.Normal);
    public bool RequireAck { get; set; }
    public string? SoundKey { get; set; }
    public string? ActionUrl { get; set; }
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>
    /// Target permissions — notification will be pushed to SignalR groups for each permission.
    /// </summary>
    public List<string> TargetPermissions { get; set; } = new();

    /// <summary>
    /// Target user IDs — for user-specific notifications (e.g. shift assigned).
    /// </summary>
    public List<long> TargetUserIds { get; set; } = new();
}
