using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Api.Hubs;

[Authorize]
public class RestaurantHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst("user_id")?.Value;
        if (!string.IsNullOrWhiteSpace(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");
        }

        // Add to permission-based groups so notifications can be targeted by permission
        var permissions = Context.User?.FindAll("permission") ?? Enumerable.Empty<Claim>();
        foreach (var perm in permissions)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"perm:{perm.Value}");
        }

        await base.OnConnectedAsync();
    }
}
