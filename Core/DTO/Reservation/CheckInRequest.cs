using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO.Reservation
{
    public class AssignTableRequest
    {
        public List<long>? TableIds { get; set; } = new List<long>();
    }
}
