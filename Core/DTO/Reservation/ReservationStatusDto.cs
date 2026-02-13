using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO.Reservation
{
    public class ReservationStatusDto
    {
        public long StatusId { get; set; }   
        public string StatusName { get; set; } = string.Empty; // Pending, Confirmed...
        public string StatusCode { get; set; } = string.Empty; // Map với enum hoặc mã định danh
    }
}
