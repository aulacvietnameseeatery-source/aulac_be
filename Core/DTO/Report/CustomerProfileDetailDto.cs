using System.Collections.Generic;
namespace Core.DTO.Report
{
    public class CustomerProfileDetailDto
{
    public decimal TotalSpent { get; set; }
    public int TotalOrders { get; set; }
    public decimal AvgOrder { get; set; }
    public int Points { get; set; }
    public List<CategoryPreferenceDto> Preferences { get; set; } = new();
    public List<RecentOrderDto> RecentOrders { get; set; } = new();
    }
}