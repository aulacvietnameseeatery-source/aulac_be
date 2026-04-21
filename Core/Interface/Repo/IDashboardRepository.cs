using Core.DTO.Dashboard;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Interface.Repo;

public interface IDashboardRepository
{
    // 1. Thống kê Summary 
    Task<(int OrderCount, decimal TotalSales, int ReservationCount)> GetSummaryDataAsync(
        DateTime startDate,
        DateTime endDate,
        uint completedOrderStatusId,
        uint completedReservationStatusId,
        CancellationToken ct = default);

    // 2. Biểu đồ doanh thu
    Task<List<RevenueChartItemDto>> GetRevenueChartAsync(
        DateTime startDate,
        DateTime endDate,
        uint completedOrderStatusId,
        CancellationToken ct = default);

    // 3. Top món bán chạy
    Task<List<TopSellingItemDto>> GetTopSellingAsync(
        DateTime startDate,
        DateTime endDate,
        uint completedOrderStatusId,
        uint imageMediaTypeId,
        int limit,
        CancellationToken ct = default);

    // 4. Thống kê ( Source & Top Customer)
    Task<DashboardStatisticsDto> GetStatisticsAsync(
        DateTime startDate,
        DateTime endDate,
        uint completedOrderStatusId,
        CancellationToken ct = default);

    Task<LiveOperationsSnapshotDto> GetLiveOperationsSnapshotAsync(
        LiveOperationsSnapshotQueryWindow window,
        LiveOperationsLookupIds lookupIds,
        CancellationToken ct = default);
}