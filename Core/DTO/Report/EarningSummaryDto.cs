using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO.Report
{
    public class EarningSummaryDto
    {
        public decimal GrossRevenue { get; set; } 
        public decimal NetRevenue { get; set; }   
        public decimal TotalTax { get; set; }
        public decimal TotalDiscount { get; set; }
    }
}
