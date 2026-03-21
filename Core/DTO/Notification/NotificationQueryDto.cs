namespace Core.DTO.Notification;

/// <summary>
/// Query parameters for the notification history endpoint.
/// </summary>
public class NotificationQueryDto
{
    public int Skip { get; set; } = 0;
    public int Take { get; set; } = 20;
    public string? Type { get; set; }
    public bool UnreadOnly { get; set; } = false;
}
