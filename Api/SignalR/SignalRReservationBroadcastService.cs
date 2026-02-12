using Api.Hubs;
using Core.Interface.Service.Others;
using Microsoft.AspNetCore.SignalR;

namespace Api.SignalR;

public class SignalRReservationBroadcastService : IReservationBroadcastService
{
    private readonly IHubContext<ReservationHub> _hubContext;

    public SignalRReservationBroadcastService(IHubContext<ReservationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task BroadcastTableLockedAsync(long tableId, DateTime lockedUntil)
    {
        await _hubContext.Clients.All.SendAsync("TableLocked", new { tableId, lockedUntil });
    }

    public async Task BroadcastTableUnlockedAsync(long tableId)
    {
        await _hubContext.Clients.All.SendAsync("TableUnlocked", new { tableId });
    }

    public async Task BroadcastReservationCreatedAsync(long reservationId, long tableId)
    {
        await _hubContext.Clients.All.SendAsync("ReservationCreated", new { reservationId, tableId });
    }

    public async Task BroadcastReservationStatusChangedAsync(long reservationId, string status)
    {
        await _hubContext.Clients.All.SendAsync("ReservationStatusChanged", new { reservationId, status });
    }
}
