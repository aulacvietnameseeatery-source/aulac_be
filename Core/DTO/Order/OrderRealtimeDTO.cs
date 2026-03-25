using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO.Order
{
    public class OrderRealtimeDTO
    {
        public long OrderId { get; set; }
        public string Status { get; set; } = default!;
        public long? TableId { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
