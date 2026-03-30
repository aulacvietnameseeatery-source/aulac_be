using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO.Report
{
    public class PaymentMethodRevenueDto
    {
        public string MethodName { get; set; } = null!;
        public decimal Amount { get; set; }
        public double Percentage { get; set; }
    }
}
