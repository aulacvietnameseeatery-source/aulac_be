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
                Title = x.n.Title,
                Body = x.n.Body,
                Priority = x.n.Priority,
                RequireAck = x.n.RequireAck,
                SoundKey = x.n.SoundKey,
                ActionUrl = x.n.ActionUrl,
                EntityType = x.n.EntityType,
                EntityId = x.n.EntityId,
                CreatedAt = x.n.CreatedAt,
                IsRead = x.rs != null && x.rs.IsRead,
                IsAcknowledged = x.rs != null && x.rs.IsAcknowledged,
                AcknowledgedAt = x.rs != null ? x.rs.AcknowledgedAt : null
            })
            .ToListAsync(ct);

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

        return await baseQuery
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
                Title = x.n.Title,
                Body = x.n.Body,
                Priority = x.n.Priority,
                RequireAck = x.n.RequireAck,
                SoundKey = x.n.SoundKey,
                ActionUrl = x.n.ActionUrl,
                EntityType = x.n.EntityType,
                EntityId = x.n.EntityId,
                CreatedAt = x.n.CreatedAt,
                IsRead = x.rs != null && x.rs.IsRead,
                IsAcknowledged = x.rs != null && x.rs.IsAcknowledged,
                AcknowledgedAt = x.rs != null ? x.rs.AcknowledgedAt : null
            })
            .ToListAsync(ct);
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
    /// </summary>
    private IQueryable<Notification> BuildUserNotificationQuery(List<string> permissions, long userId)
    {
        var userIdStr = userId.ToString();

        return _context.Notifications
            .Where(n =>
                // User-specific targeting
                (n.TargetUserIds != null && n.TargetUserIds.Contains(userIdStr))
                // Permission-based targeting: check if any user permission matches
                || (n.TargetPermissions != null && permissions.Any(p => n.TargetPermissions.Contains(p)))
                // Broadcast (no targeting = everyone)
                || (n.TargetPermissions == null && n.TargetUserIds == null));
    }
}
