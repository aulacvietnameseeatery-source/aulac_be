using Api.Hubs;
using Core.DTO.Order;
using Core.Interface.Service.Others;
using Microsoft.AspNetCore.SignalR;
using Org.BouncyCastle.Ocsp;

namespace Api.SignalR
{
    public class OrderRealtimeService : IOrderRealtimeService
    {
        private readonly IHubContext<RestaurantHub> _hub;

        public OrderRealtimeService(IHubContext<RestaurantHub> hub)
        {
            _hub = hub;
        }

        public async Task OrderCreatedAsync(OrderRealtimeDTO data)
        {
            await _hub.Clients.Group("orders")
                .SendAsync("OrderCreated", data);
        }

        public async Task OrderUpdatedAsync(OrderRealtimeDTO data)
        {
            await _hub.Clients.Group("orders")
                .SendAsync("OrderUpdated", data);

            await _hub.Clients.Group($"order-{data.OrderId}")
                .SendAsync("OrderDetailUpdated", data);
        }

        public async Task OrderItemUpdatedAsync(OrderItemRealtimeDTO data)
        {
            await _hub.Clients.Group($"order-{data.OrderId}")
                .SendAsync("OrderItemUpdated", data);
        }
    }
}
