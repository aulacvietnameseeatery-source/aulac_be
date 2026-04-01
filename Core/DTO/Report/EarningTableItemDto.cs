using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO.Report
{
    public class EarningTableItemDto
    {
        public string Date { get; set; } = null!; 
        public int TotalOrders { get; set; }
        public decimal GrossRevenue { get; set; }
        public decimal NetRevenue { get; set; }
        public decimal TotalTax { get; set; }
    }
}
