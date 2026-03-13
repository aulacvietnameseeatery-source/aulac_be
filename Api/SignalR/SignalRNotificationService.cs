using Api.Hubs;
using Core.Interface.Service.Others;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infa.Service
{
    public class SignalRNotificationService : IRealtimeNotificationService
    {

        private readonly IHubContext<ReservationHub> _hubContext;

        public SignalRNotificationService(IHubContext<ReservationHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task NotifyTableStatusChangedAsync(long tableId, string newStatus)
        {
            await _hubContext.Clients.All.SendAsync("TableStatusChanged", tableId, newStatus);
        }

        public async Task NotifyReservationUpdatedAsync(long reservationId, string newStatus)
        {
            await _hubContext.Clients.All.SendAsync("ReservationUpdated", reservationId, newStatus);
        }
    }
}
