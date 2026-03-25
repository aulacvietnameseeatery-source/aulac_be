using Core.DTO.Notification;

namespace Core.Interface.Service.Notification;

public interface INotificationRealtimePublisher
{
    /// <summary>
    /// Push notification to all SignalR groups matching the given permissions.
    /// </summary>
    Task PublishToPermissionsAsync(IEnumerable<string> permissions, NotificationDto dto, CancellationToken ct = default);

    /// <summary>
    /// Push notification to a specific user's SignalR group.
    /// </summary>
    Task PublishToUserAsync(long userId, NotificationDto dto, CancellationToken ct = default);

    /// <summary>
    /// Push notification to all connected clients.
    /// </summary>
    Task PublishToAllAsync(NotificationDto dto, CancellationToken ct = default);
}
