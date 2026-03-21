using Core.DTO.Notification;

namespace Core.Interface.Service.Notification;

public interface INotificationService
{
    Task PublishAsync(PublishNotificationRequest request, CancellationToken ct = default);

    Task<List<NotificationListItemDto>> GetNotificationsAsync(
        NotificationQueryDto query,
        IEnumerable<string> userPermissions,
        long userId,
        CancellationToken ct = default);

    Task<int> GetUnreadCountAsync(
        IEnumerable<string> userPermissions,
        long userId,
        CancellationToken ct = default);

    Task<List<NotificationListItemDto>> GetMissedAsync(
        IEnumerable<string> userPermissions,
        long userId,
        DateTime? afterUtc,
        CancellationToken ct = default);

    Task MarkAsReadAsync(long notificationId, long userId, CancellationToken ct = default);

    Task MarkAllReadAsync(IEnumerable<string> userPermissions, long userId, CancellationToken ct = default);

    Task AcknowledgeAsync(long notificationId, long userId, CancellationToken ct = default);

    // --- Notification Preferences ---

    Task<List<NotificationPreferenceDto>> GetPreferencesAsync(long userId, CancellationToken ct = default);

    Task UpdatePreferencesAsync(long userId, UpdateNotificationPreferencesRequest request, CancellationToken ct = default);
}
