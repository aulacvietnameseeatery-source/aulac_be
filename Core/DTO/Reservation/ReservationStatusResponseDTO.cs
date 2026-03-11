using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO.Reservation
{
    public class ReservationStatusResponseDTO
    {
        public long ReservationId { get; set; }

        public ReservationStatusCode Status { get; set; }

        public long? CreatedOrderId { get; set; }
    }
}
