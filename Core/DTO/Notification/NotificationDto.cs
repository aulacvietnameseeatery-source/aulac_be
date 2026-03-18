using Core.Enum;

namespace Core.DTO.Notification;

/// <summary>
/// DTO pushed to clients via SignalR in real-time.
/// </summary>
public class NotificationDto
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
    public Dictionary<string, object>? Metadata { get; set; }
    public DateTime CreatedAt { get; set; }
}
