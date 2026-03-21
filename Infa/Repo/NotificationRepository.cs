using System.Linq.Expressions;
using Core.DTO.Notification;
using Core.Entity;
using Core.Interface.Repo;
using Infa.Data;
using Microsoft.EntityFrameworkCore;

namespace Infa.Repo;

public class NotificationRepository : INotificationRepository
{
    private readonly RestaurantMgmtContext _context;

    public NotificationRepository(RestaurantMgmtContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Notification notification, CancellationToken ct = default)
    {
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<List<NotificationListItemDto>> GetByUserAsync(
        IEnumerable<string> userPermissions,
        long userId,
        NotificationQueryDto query,
        CancellationToken ct = default)
    {
        var permList = userPermissions.ToList();

        var baseQuery = BuildUserNotificationQuery(permList, userId);

        if (!string.IsNullOrWhiteSpace(query.Type))
            baseQuery = baseQuery.Where(n => n.Type == query.Type);

        if (query.UnreadOnly)
        {
            baseQuery = baseQuery.Where(n =>
                !_context.NotificationReadStates.Any(rs =>
                    rs.NotificationId == n.NotificationId && rs.UserId == userId && rs.IsRead));
        }

        var items = await baseQuery
            .OrderByDescending(n => n.CreatedAt)
            .Skip(query.Skip)
            .Take(query.Take)
            .GroupJoin(
                _context.NotificationReadStates.Where(rs => rs.UserId == userId),
                n => n.NotificationId,
                rs => rs.NotificationId,
                (n, states) => new { n, rs = states.FirstOrDefault() })
            .Select(x => new NotificationListItemDto
            {
                Id = x.n.NotificationId,
                Type = x.n.Type,
                Title = x.n.Type,
                Body = null,
                Priority = x.n.Priority,
                RequireAck = x.n.RequireAck,
                SoundKey = x.n.SoundKey,
                ActionUrl = x.n.ActionUrl,
                EntityType = x.n.EntityType,
                EntityId = x.n.EntityId,
                MetadataJson = x.n.MetadataJson,
                CreatedAt = x.n.CreatedAt,
                IsRead = x.rs != null && x.rs.IsRead,
                IsAcknowledged = x.rs != null && x.rs.IsAcknowledged,
                AcknowledgedAt = x.rs != null ? x.rs.AcknowledgedAt : null
            })
            .ToListAsync(ct);

        foreach (var item in items) item.HydrateMetadata();
        return items;
    }

    public async Task<int> GetUnreadCountAsync(
        IEnumerable<string> userPermissions,
        long userId,
        CancellationToken ct = default)
    {
        var permList = userPermissions.ToList();

        var baseQuery = BuildUserNotificationQuery(permList, userId);

        return await baseQuery
            .Where(n => !_context.NotificationReadStates.Any(rs =>
                rs.NotificationId == n.NotificationId && rs.UserId == userId && rs.IsRead))
            .CountAsync(ct);
    }

    public async Task<List<NotificationListItemDto>> GetMissedAsync(
        IEnumerable<string> userPermissions,
        long userId,
        DateTime? afterUtc,
        CancellationToken ct = default)
    {
        var permList = userPermissions.ToList();

        var baseQuery = BuildUserNotificationQuery(permList, userId);

        if (afterUtc.HasValue)
            baseQuery = baseQuery.Where(n => n.CreatedAt > afterUtc.Value);

        var items = await baseQuery
            .OrderByDescending(n => n.CreatedAt)
            .Take(100)
            .GroupJoin(
                _context.NotificationReadStates.Where(rs => rs.UserId == userId),
                n => n.NotificationId,
                rs => rs.NotificationId,
                (n, states) => new { n, rs = states.FirstOrDefault() })
            .Select(x => new NotificationListItemDto
            {
                Id = x.n.NotificationId,
                Type = x.n.Type,
                Title = x.n.Type,
                Body = null,
                Priority = x.n.Priority,
                RequireAck = x.n.RequireAck,
                SoundKey = x.n.SoundKey,
                ActionUrl = x.n.ActionUrl,
                EntityType = x.n.EntityType,
                EntityId = x.n.EntityId,
                MetadataJson = x.n.MetadataJson,
                CreatedAt = x.n.CreatedAt,
                IsRead = x.rs != null && x.rs.IsRead,
                IsAcknowledged = x.rs != null && x.rs.IsAcknowledged,
                AcknowledgedAt = x.rs != null ? x.rs.AcknowledgedAt : null
            })
            .ToListAsync(ct);

        foreach (var item in items) item.HydrateMetadata();
        return items;
    }

    public async Task MarkAsReadAsync(long notificationId, long userId, CancellationToken ct = default)
    {
        var readState = await _context.NotificationReadStates
            .FirstOrDefaultAsync(rs => rs.NotificationId == notificationId && rs.UserId == userId, ct);

        if (readState == null)
        {
            readState = new NotificationReadState
            {
                NotificationId = notificationId,
                UserId = userId,
                IsRead = true,
                ReadAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };
            _context.NotificationReadStates.Add(readState);
        }
        else if (!readState.IsRead)
        {
            readState.IsRead = true;
            readState.ReadAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(ct);
    }

    public async Task MarkAllReadAsync(IEnumerable<string> userPermissions, long userId, CancellationToken ct = default)
    {
        var permList = userPermissions.ToList();

        // Get IDs of unread notifications visible to this user
        var unreadNotificationIds = await BuildUserNotificationQuery(permList, userId)
            .Where(n => !_context.NotificationReadStates.Any(rs =>
                rs.NotificationId == n.NotificationId && rs.UserId == userId && rs.IsRead))
            .Select(n => n.NotificationId)
            .ToListAsync(ct);

        if (unreadNotificationIds.Count == 0) return;

        // Get existing read states for these notifications
        var existingStates = await _context.NotificationReadStates
            .Where(rs => unreadNotificationIds.Contains(rs.NotificationId) && rs.UserId == userId)
            .ToListAsync(ct);

        var existingIds = existingStates.Select(rs => rs.NotificationId).ToHashSet();

        // Update existing unread states
        foreach (var state in existingStates.Where(s => !s.IsRead))
        {
            state.IsRead = true;
            state.ReadAt = DateTime.UtcNow;
        }

        // Create new read states for notifications without one
        var newStates = unreadNotificationIds
            .Where(id => !existingIds.Contains(id))
            .Select(id => new NotificationReadState
            {
                NotificationId = id,
                UserId = userId,
                IsRead = true,
                ReadAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            });

        _context.NotificationReadStates.AddRange(newStates);
        await _context.SaveChangesAsync(ct);
    }

    public async Task AcknowledgeAsync(long notificationId, long userId, CancellationToken ct = default)
    {
        var readState = await _context.NotificationReadStates
            .FirstOrDefaultAsync(rs => rs.NotificationId == notificationId && rs.UserId == userId, ct);

        if (readState == null)
        {
            readState = new NotificationReadState
            {
                NotificationId = notificationId,
                UserId = userId,
                IsRead = true,
                ReadAt = DateTime.UtcNow,
                IsAcknowledged = true,
                AcknowledgedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };
            _context.NotificationReadStates.Add(readState);
        }
        else if (!readState.IsAcknowledged)
        {
            readState.IsAcknowledged = true;
            readState.AcknowledgedAt = DateTime.UtcNow;
            if (!readState.IsRead)
            {
                readState.IsRead = true;
                readState.ReadAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Build a base query for notifications visible to a user based on their permissions or direct targeting.
    /// Pomelo MySQL does not support primitive collection translation, so permission matching
    /// is built as an expression tree with individual OR conditions per permission.
    /// </summary>
    private IQueryable<Notification> BuildUserNotificationQuery(List<string> permissions, long userId)
    {
        var userIdStr = userId.ToString();

        // n => ...
        var param = Expression.Parameter(typeof(Notification), "n");

        // n.TargetUserIds != null && n.TargetUserIds.Contains(userIdStr)
        var targetUserIds = Expression.Property(param, nameof(Notification.TargetUserIds));
        var userTargeted = Expression.AndAlso(
            Expression.NotEqual(targetUserIds, Expression.Constant(null, typeof(string))),
            Expression.Call(targetUserIds, typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) })!,
                Expression.Constant(userIdStr)));

        // n.TargetPermissions == null && n.TargetUserIds == null
        var targetPerms = Expression.Property(param, nameof(Notification.TargetPermissions));
        var broadcast = Expression.AndAlso(
            Expression.Equal(targetPerms, Expression.Constant(null, typeof(string))),
            Expression.Equal(targetUserIds, Expression.Constant(null, typeof(string))));

        Expression body = Expression.OrElse(userTargeted, broadcast);

        // For each permission, add: n.TargetPermissions != null && n.TargetPermissions.Contains("PERM")
        if (permissions.Count > 0)
        {
            var containsMethod = typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) })!;
            foreach (var perm in permissions)
            {
                var permCheck = Expression.AndAlso(
                    Expression.NotEqual(targetPerms, Expression.Constant(null, typeof(string))),
                    Expression.Call(targetPerms, containsMethod, Expression.Constant(perm)));
                body = Expression.OrElse(body, permCheck);
            }
        }

        var predicate = Expression.Lambda<Func<Notification, bool>>(body, param);
        return _context.Notifications.Where(predicate);
    }

    // --- Notification Preferences ---

    public async Task<List<NotificationPreference>> GetPreferencesAsync(long userId, CancellationToken ct = default)
    {
        return await _context.NotificationPreferences
            .Where(p => p.UserId == userId)
            .OrderBy(p => p.NotificationType)
            .ToListAsync(ct);
    }

    public async Task UpsertPreferencesAsync(long userId, List<NotificationPreference> preferences, CancellationToken ct = default)
    {
        var existing = await _context.NotificationPreferences
            .Where(p => p.UserId == userId)
            .ToListAsync(ct);

        var existingMap = existing.ToDictionary(p => p.NotificationType);

        foreach (var pref in preferences)
        {
            if (existingMap.TryGetValue(pref.NotificationType, out var found))
            {
                found.IsEnabled = pref.IsEnabled;
                found.SoundEnabled = pref.SoundEnabled;
                found.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _context.NotificationPreferences.Add(new NotificationPreference
                {
                    UserId = userId,
                    NotificationType = pref.NotificationType,
                    IsEnabled = pref.IsEnabled,
                    SoundEnabled = pref.SoundEnabled,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        await _context.SaveChangesAsync(ct);
    }

    public async Task<HashSet<string>> GetDisabledTypesAsync(long userId, CancellationToken ct = default)
    {
        var disabled = await _context.NotificationPreferences
            .Where(p => p.UserId == userId && !p.IsEnabled)
            .Select(p => p.NotificationType)
            .ToListAsync(ct);

        return disabled.ToHashSet();
    }
}
