using Core.DTO.Notification;

namespace Core.Interface.Repo;

public interface INotificationRepository
{
    Task AddAsync(Core.Entity.Notification notification, CancellationToken ct = default);

    Task<List<NotificationListItemDto>> GetByUserAsync(
        IEnumerable<string> userPermissions,
        long userId,
        NotificationQueryDto query,
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

    Task<List<Core.Entity.NotificationPreference>> GetPreferencesAsync(long userId, CancellationToken ct = default);

    Task UpsertPreferencesAsync(long userId, List<Core.Entity.NotificationPreference> preferences, CancellationToken ct = default);

    /// <summary>
    /// Returns the set of notification types the user has explicitly disabled.
    /// </summary>
    Task<HashSet<string>> GetDisabledTypesAsync(long userId, CancellationToken ct = default);
}
