using System.Text.Json;
using System.Text.Json.Serialization;

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
    public Dictionary<string, object>? Metadata { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }
    public bool IsAcknowledged { get; set; }
    public DateTime? AcknowledgedAt { get; set; }

    /// <summary>
    /// Internal: used by EF projection, then deserialized into Metadata.
    /// </summary>
    [JsonIgnore]
    public string? MetadataJson { get; set; }

    /// <summary>
    /// Call after EF projection to hydrate the Metadata dictionary from MetadataJson.
    /// </summary>
    public void HydrateMetadata()
    {
        if (MetadataJson != null && Metadata == null)
            Metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(MetadataJson);
    }
}
