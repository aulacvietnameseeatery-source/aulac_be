using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO.Order
{
    public class RecentOrderDTO
    {
        public long OrderId { get; set; }

        public string CustomerName { get; set; } = null!;

        public string Source { get; set; } = null!;

        public string? TableCode { get; set; }

        public DateTime CreatedAt { get; set; }

        public string Status { get; set; } = null!;
    }
}
