using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO.Reservation
{
    public class ManualTableAvailabilityDto
    {
        public long TableId { get; set; }
        public string TableCode { get; set; } = null!;
        public int Capacity { get; set; }
        public string TableType { get; set; } = null!;
        public string Zone { get; set; } = null!;
    }
}
