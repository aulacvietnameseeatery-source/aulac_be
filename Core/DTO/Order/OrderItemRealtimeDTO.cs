using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO.Order
{
    public class OrderItemRealtimeDTO
    {
        public long OrderItemId { get; set; }
        public long OrderId { get; set; }
        public string Status { get; set; } = default!;
        public DateTime UpdatedAt { get; set; }
    }
}
