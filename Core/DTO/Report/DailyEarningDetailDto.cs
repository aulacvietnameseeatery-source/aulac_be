using System.Collections.Generic;

namespace Core.DTO.Report
{
    public class DailyEarningDetailDto
    {
        public decimal TotalNet { get; set; }
        public decimal AvgOrder { get; set; }
        public int TotalOrders { get; set; }
        public List<HourlyRevenueDto> HourlyRevenue { get; set; } = new();
        public List<RecentOrderDto> RecentOrders { get; set; } = new();
    }
}