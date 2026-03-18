namespace Core.DTO.Notification;

/// <summary>
/// DTO for notification history list items (includes per-user read/ack state).
/// </summary>
public class NotificationListItemDto
{
    public long Id { get; set; }
    public string Type { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string? Body { get; set; }
    public string Priority { get; set; } = null!;
    public bool RequireAck { get; set; }
    public string? SoundKey { get; set; }
    public string? ActionUrl { get; set; }
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }
    public bool IsAcknowledged { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
}
