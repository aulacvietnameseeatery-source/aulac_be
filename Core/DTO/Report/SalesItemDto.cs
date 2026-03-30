using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO.Report
{
    public class SalesItemDto
    {
        public long DishId { get; set; }
        public string DishName { get; set; } = null!;
        public string CategoryName { get; set; } = null!;
        public int QuantitySold { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}
