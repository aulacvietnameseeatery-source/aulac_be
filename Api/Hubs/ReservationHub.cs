using Microsoft.AspNetCore.SignalR;

namespace Api.Hubs;

public class ReservationHub : Hub
{
    // Implementation can be expanded later if clients need to send messages to the server
    // For now, it's primarily for server-to-client broadcasting
    public async Task JoinOrders()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "orders");
    }

    public async Task JoinOrder(long orderId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"order-{orderId}");
    }
}
