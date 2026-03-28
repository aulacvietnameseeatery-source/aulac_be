using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO.Dashboard
{
    public class DashboardSummaryDto
    {
        public TrendValueDto TotalOrders { get; set; } = new();
        public TrendValueDto TotalSales { get; set; } = new();
        public TrendValueDto AverageOrderValue { get; set; } = new();
        public TrendValueDto TotalReservations { get; set; } = new();
    }
}
