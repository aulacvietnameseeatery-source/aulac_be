using Api.Hubs;
using Core.DTO.Notification;
using Core.Interface.Service.Notification;
using Microsoft.AspNetCore.SignalR;

namespace Api.SignalR;

public class NotificationRealtimePublisher : INotificationRealtimePublisher
{
    private readonly IHubContext<RestaurantHub> _hubContext;

    public NotificationRealtimePublisher(IHubContext<RestaurantHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task PublishToPermissionsAsync(IEnumerable<string> permissions, NotificationDto dto, CancellationToken ct = default)
    {
        var tasks = permissions.Distinct().Select(perm =>
            _hubContext.Clients.Group($"perm:{perm}")
                .SendAsync("ReceiveNotification", dto, ct));

        await Task.WhenAll(tasks);
    }

    public Task PublishToUserAsync(long userId, NotificationDto dto, CancellationToken ct = default)
    {
        return _hubContext.Clients.Group($"user:{userId}")
            .SendAsync("ReceiveNotification", dto, ct);
    }

    public Task PublishToAllAsync(NotificationDto dto, CancellationToken ct = default)
    {
        return _hubContext.Clients.All
            .SendAsync("ReceiveNotification", dto, ct);
    }
}
