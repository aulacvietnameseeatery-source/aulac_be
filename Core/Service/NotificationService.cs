using System.Text.Json;
using Core.DTO.Notification;
using Core.Interface.Repo;
using Core.Interface.Service.Notification;
using Microsoft.Extensions.Logging;

namespace Core.Service;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly INotificationRealtimePublisher _realtimePublisher;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        INotificationRepository notificationRepository,
        INotificationRealtimePublisher realtimePublisher,
        ILogger<NotificationService> logger)
    {
        _notificationRepository = notificationRepository;
        _realtimePublisher = realtimePublisher;
        _logger = logger;
    }

    public async Task PublishAsync(PublishNotificationRequest request, CancellationToken ct = default)
    {
        // Metadata-first payload: FE localizes by Type + Metadata.
        // Keep compact title for backward compatibility and set body to null.
        var compactTitle = request.Type;

        var notification = new Entity.Notification
        {
            Type = request.Type,
            Title = compactTitle,
            Body = null,
            Priority = request.Priority,
            RequireAck = request.RequireAck,
            SoundKey = request.SoundKey,
            ActionUrl = request.ActionUrl,
            EntityType = request.EntityType,
            EntityId = request.EntityId,
            MetadataJson = request.Metadata != null
                ? JsonSerializer.Serialize(request.Metadata)
                : null,
            TargetPermissions = request.TargetPermissions.Count > 0
                ? string.Join(",", request.TargetPermissions)
                : null,
            TargetUserIds = request.TargetUserIds.Count > 0
                ? string.Join(",", request.TargetUserIds)
                : null,
            CreatedAt = DateTime.UtcNow
        };

        await _notificationRepository.AddAsync(notification, ct);

        // Build the real-time DTO
        var dto = new NotificationDto
        {
            Id = notification.NotificationId,
            Type = notification.Type,
            Title = compactTitle,
            Body = null,
            Priority = notification.Priority,
            RequireAck = notification.RequireAck,
            SoundKey = notification.SoundKey,
            ActionUrl = notification.ActionUrl,
            EntityType = notification.EntityType,
            EntityId = notification.EntityId,
            Metadata = request.Metadata,
            CreatedAt = notification.CreatedAt
        };

        // Push to real-time channels
        try
        {
            if (request.TargetPermissions.Count > 0)
                await _realtimePublisher.PublishToPermissionsAsync(request.TargetPermissions, dto, ct);

            foreach (var userId in request.TargetUserIds)
                await _realtimePublisher.PublishToUserAsync(userId, dto, ct);

            // If no specific targets, broadcast to all
            if (request.TargetPermissions.Count == 0 && request.TargetUserIds.Count == 0)
                await _realtimePublisher.PublishToAllAsync(dto, ct);
        }
        catch (Exception ex)
        {
            // Real-time push failure should not fail the operation.
            // The notification is already persisted; clients can recover via missed-notification fetch.
            _logger.LogWarning(ex, "Failed to push real-time notification {NotificationId}", notification.NotificationId);
        }
    }

    public Task<List<NotificationListItemDto>> GetNotificationsAsync(
        NotificationQueryDto query,
        IEnumerable<string> userPermissions,
        long userId,
        CancellationToken ct = default)
    {
        return _notificationRepository.GetByUserAsync(userPermissions, userId, query, ct);
    }

    public Task<int> GetUnreadCountAsync(
        IEnumerable<string> userPermissions,
        long userId,
        CancellationToken ct = default)
    {
        return _notificationRepository.GetUnreadCountAsync(userPermissions, userId, ct);
    }

    public Task<List<NotificationListItemDto>> GetMissedAsync(
        IEnumerable<string> userPermissions,
        long userId,
        DateTime? afterUtc,
        CancellationToken ct = default)
    {
        return _notificationRepository.GetMissedAsync(userPermissions, userId, afterUtc, ct);
    }

    public Task MarkAsReadAsync(long notificationId, long userId, CancellationToken ct = default)
    {
        return _notificationRepository.MarkAsReadAsync(notificationId, userId, ct);
    }

    public Task MarkAllReadAsync(IEnumerable<string> userPermissions, long userId, CancellationToken ct = default)
    {
        return _notificationRepository.MarkAllReadAsync(userPermissions, userId, ct);
    }

    public Task AcknowledgeAsync(long notificationId, long userId, CancellationToken ct = default)
    {
        return _notificationRepository.AcknowledgeAsync(notificationId, userId, ct);
    }

    // --- Notification Preferences ---

    public async Task<List<NotificationPreferenceDto>> GetPreferencesAsync(long userId, CancellationToken ct = default)
    {
        var entities = await _notificationRepository.GetPreferencesAsync(userId, ct);

        // Return all known types, merging persisted preferences with defaults
        var prefMap = entities.ToDictionary(p => p.NotificationType);

        var allTypes = System.Enum.GetNames(typeof(Enum.NotificationType));

        return allTypes.Select(type =>
        {
            if (prefMap.TryGetValue(type, out var pref))
            {
                return new NotificationPreferenceDto
                {
                    NotificationType = type,
                    IsEnabled = pref.IsEnabled,
                    SoundEnabled = pref.SoundEnabled
                };
            }

            // Default: enabled
            return new NotificationPreferenceDto
            {
                NotificationType = type,
                IsEnabled = true,
                SoundEnabled = true
            };
        }).ToList();
    }

    public async Task UpdatePreferencesAsync(long userId, UpdateNotificationPreferencesRequest request, CancellationToken ct = default)
    {
        var entities = request.Preferences.Select(p => new Entity.NotificationPreference
        {
            UserId = userId,
            NotificationType = p.NotificationType,
            IsEnabled = p.IsEnabled,
            SoundEnabled = p.SoundEnabled
        }).ToList();

        await _notificationRepository.UpsertPreferencesAsync(userId, entities, ct);
    }
}
