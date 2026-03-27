using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO.Payment
{
    public class PaymentListDTO
    {
        public long PaymentId { get; set; }

        public long OrderId { get; set; }

        public decimal ReceivedAmount { get; set; }

        public decimal ChangeAmount { get; set; }

        public decimal FinalAmount { get; set; }

        public string Method { get; set; } = null!;

        public DateTime? PaidAt { get; set; }

        public string? CustomerName { get; set; }

        public string? CustomerPhone { get; set; }
    }
}
