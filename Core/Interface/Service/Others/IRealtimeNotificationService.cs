using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interface.Service.Others
{
    public interface IRealtimeNotificationService
    {
        Task NotifyTableStatusChangedAsync(long tableId, string newStatus);
        Task NotifyReservationUpdatedAsync(long reservationId, string newStatus);
    }
}
