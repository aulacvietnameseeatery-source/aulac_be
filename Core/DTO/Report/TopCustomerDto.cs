using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO.Report
{
    public class TopCustomerDto
    {
        public long CustomerId { get; set; }
        public string CustomerName { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public int TotalOrders { get; set; }
        public decimal TotalSpent { get; set; }
        public DateTime? LastVisitDate { get; set; }
    }
}
