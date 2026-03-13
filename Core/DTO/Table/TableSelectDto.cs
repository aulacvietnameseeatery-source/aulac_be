using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO.Table
{
    public class TableSelectDto
    {
        public long TableId { get; set; }
        public string TableCode { get; set; } = null!;
        public int Capacity { get; set; }
        public uint ZoneId { get; set; }
        public string ZoneName { get; set; } = string.Empty;
        public string StatusCode { get; set; } = null!;

        public string TableType { get; set; } = null!;

        public bool HasActiveOrder { get; set; }
        public long? ActiveOrderId { get; set; }

        public DateTime? UpcomingReservationTime { get; set; }
    }
}
