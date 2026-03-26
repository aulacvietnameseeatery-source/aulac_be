using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace Api.Hubs;

/// <summary>
/// Unified hub for all real-time features: notifications, orders, reservations, shifts.
/// Anonymous connections are allowed (customer ordering flow).
/// Permission/user groups are only assigned for authenticated connections.
/// </summary>
public class RestaurantHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        // Only assign user/permission groups for authenticated connections
        var userId = Context.User?.FindFirst("user_id")?.Value;
        if (!string.IsNullOrWhiteSpace(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");

            // Add to permission-based groups so notifications can be targeted by permission
            var permissions = Context.User?.FindAll("permission") ?? Enumerable.Empty<Claim>();
            foreach (var perm in permissions)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"perm:{perm.Value}");
            }
        }

        await base.OnConnectedAsync();
    }

    // ── Order / Reservation group methods (merged from ReservationHub) ──

    public async Task JoinOrders()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "orders");
    }

    public async Task JoinOrder(long orderId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"order-{orderId}");
    }
}
