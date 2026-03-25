using System;

namespace Core.Entity;

public partial class Notification
{
    public long NotificationId { get; set; }

    public string Type { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string? Body { get; set; }

    public string Priority { get; set; } = null!;

    public bool RequireAck { get; set; }

    public string? SoundKey { get; set; }

    public string? ActionUrl { get; set; }

    public string? EntityType { get; set; }

    public string? EntityId { get; set; }

    public string? MetadataJson { get; set; }

    /// <summary>
    /// Comma-separated permission codes for targeting, e.g. "ORDER:READ,ORDER:EDIT"
    /// </summary>
    public string? TargetPermissions { get; set; }

    /// <summary>
    /// Comma-separated user IDs for user-specific notifications, e.g. "1,42"
    /// </summary>
    public string? TargetUserIds { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<NotificationReadState> ReadStates { get; set; } = new List<NotificationReadState>();
}
