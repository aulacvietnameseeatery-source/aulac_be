using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO.Dashboard
{
    public class DashboardStatisticsDto
    {
        public Dictionary<string, int> OrdersByType { get; set; } = new();
        public TopCustomerDto? TopCustomers { get; set; }
    }
}
