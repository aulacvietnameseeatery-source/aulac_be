using System.Collections.Generic;
namespace Core.DTO.Report { 
public class DishPerformanceDetailDto
{
    public decimal TotalRevenue { get; set; }
    public int TotalSold { get; set; }
    public double AvgDailySold { get; set; }
    public List<SalesTrendDto> Trend { get; set; } = new();
    public List<CrossSellItemDto> FrequentlyBoughtWith { get; set; } = new();
}
}