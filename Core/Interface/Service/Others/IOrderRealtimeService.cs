using Core.DTO.Order;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interface.Service.Others
{
    public interface IOrderRealtimeService
    {
        Task OrderCreatedAsync(OrderRealtimeDTO data);
        Task OrderUpdatedAsync(OrderRealtimeDTO data);
        Task OrderItemUpdatedAsync(OrderItemRealtimeDTO data);
        Task OrderPaidAsync(long orderId);
    }
}
