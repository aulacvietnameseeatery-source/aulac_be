using Api.Hubs;
using Core.Data;
using Core.DTO.Shift;
using Core.Interface.Service.Shift;
using Microsoft.AspNetCore.SignalR;

namespace Api.SignalR;

public class SignalRShiftLiveRealtimePublisher : IShiftLiveRealtimePublisher
{
    private const string ShiftLiveBoardChangedEvent = "ShiftLiveBoardChanged";
    private readonly IHubContext<RestaurantHub> _hub;

    public SignalRShiftLiveRealtimePublisher(IHubContext<RestaurantHub> hub)
    {
        _hub = hub;
    }

    public Task PublishBoardChangedAsync(ShiftLiveRealtimeEventDto payload, CancellationToken ct = default)
    {
        return _hub.Clients
            .Group($"perm:{Permissions.ViewShift}")
            .SendAsync(ShiftLiveBoardChangedEvent, payload, ct);
    }
}