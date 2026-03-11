using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO.Reservation
{
    public class UpdateReservationStatusRequest
    {
        public ReservationStatusCode Status { get; set; }
    }
}
