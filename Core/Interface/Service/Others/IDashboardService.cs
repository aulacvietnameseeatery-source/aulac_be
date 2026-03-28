using Core.DTO.Dashboard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interface.Service.Others
{
    public interface IDashboardService
    {
        Task<DashboardSummaryDto> GetSummaryAsync(DashboardFilterRequest request, CancellationToken ct = default);
        Task<List<RevenueChartItemDto>> GetRevenueChartAsync(DashboardFilterRequest request, CancellationToken ct = default);
        Task<List<TopSellingItemDto>> GetTopSellingAsync(DashboardFilterRequest request, int limit = 6, CancellationToken ct = default);
        Task<DashboardStatisticsDto> GetStatisticsAsync(DashboardFilterRequest request, CancellationToken ct = default);
    }
}
