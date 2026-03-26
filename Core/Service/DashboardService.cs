using Core.DTO.Dashboard;
using Core.Enum;
using Core.Extensions;
using Core.Interface.Repo;
using Core.Interface.Service.Entity; 
using Core.Interface.Service.Others;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LookupTypeEnum = Core.Enum.LookupType;

namespace Core.Service;

public class DashboardService : IDashboardService
{
    private readonly IDashboardRepository _dashboardRepository;
    private readonly ILookupResolver _lookupResolver;

    public DashboardService(
        IDashboardRepository dashboardRepository,
        ILookupResolver lookupResolver)
    {
        _dashboardRepository = dashboardRepository;
        _lookupResolver = lookupResolver;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(DashboardFilterRequest request, CancellationToken ct = default)
    {
        var startDate = request.StartDate ?? DateTime.UtcNow.AddDays(-7);
        var endDate = request.EndDate ?? DateTime.UtcNow;

        var timeSpan = endDate - startDate;
        var prevStartDate = startDate.Subtract(timeSpan);
        var prevEndDate = startDate;

        var completedOrderStatusId = await OrderStatusCode.COMPLETED.ToOrderStatusIdAsync(_lookupResolver, ct);

        var completedReservationStatusId = await _lookupResolver.GetIdAsync(
            (ushort)LookupTypeEnum.ReservationStatus, ReservationStatusCode.COMPLETED, ct);

        var currentData = await _dashboardRepository.GetSummaryDataAsync(
            startDate, endDate, completedOrderStatusId, completedReservationStatusId, ct);

        var prevData = await _dashboardRepository.GetSummaryDataAsync(
            prevStartDate, prevEndDate, completedOrderStatusId, completedReservationStatusId, ct);

        var currentAvg = currentData.OrderCount > 0 ? currentData.TotalSales / currentData.OrderCount : 0;
        var prevAvg = prevData.OrderCount > 0 ? prevData.TotalSales / prevData.OrderCount : 0;

        return new DashboardSummaryDto
        {
            TotalOrders = CalculateTrend(currentData.OrderCount, prevData.OrderCount),
            TotalSales = CalculateTrend(currentData.TotalSales, prevData.TotalSales),
            AverageOrderValue = CalculateTrend(currentAvg, prevAvg),
            TotalReservations = CalculateTrend(currentData.ReservationCount, prevData.ReservationCount)
        };
    }

    public async Task<List<RevenueChartItemDto>> GetRevenueChartAsync(DashboardFilterRequest request, CancellationToken ct = default)
    {
        var startDate = request.StartDate ?? DateTime.UtcNow.AddDays(-7);
        var endDate = request.EndDate ?? DateTime.UtcNow;

        var completedOrderStatusId = await OrderStatusCode.COMPLETED.ToOrderStatusIdAsync(_lookupResolver, ct);

        return await _dashboardRepository.GetRevenueChartAsync(startDate, endDate, completedOrderStatusId, ct);
    }

    public async Task<List<TopSellingItemDto>> GetTopSellingAsync(DashboardFilterRequest request, int limit = 6, CancellationToken ct = default)
    {
        var startDate = request.StartDate ?? DateTime.UtcNow.AddDays(-30);
        var endDate = request.EndDate ?? DateTime.UtcNow;

        var completedOrderStatusId = await OrderStatusCode.COMPLETED.ToOrderStatusIdAsync(_lookupResolver, ct);
        var imageMediaTypeId = await _lookupResolver.GetIdAsync((ushort)LookupTypeEnum.MediaType, MediaTypeCode.IMAGE, ct);

        return await _dashboardRepository.GetTopSellingAsync(
            startDate, endDate, completedOrderStatusId, imageMediaTypeId, limit, ct);
    }

    public async Task<DashboardStatisticsDto> GetStatisticsAsync(DashboardFilterRequest request, CancellationToken ct = default)
    {
        var startDate = request.StartDate ?? DateTime.UtcNow.AddDays(-30);
        var endDate = request.EndDate ?? DateTime.UtcNow;

        var completedOrderStatusId = await OrderStatusCode.COMPLETED.ToOrderStatusIdAsync(_lookupResolver, ct);

        return await _dashboardRepository.GetStatisticsAsync(startDate, endDate, completedOrderStatusId, ct);
    }

    private TrendValueDto CalculateTrend(decimal current, decimal previous)
    {
        if (previous == 0)
            return new TrendValueDto { Value = current, Trend = current > 0 ? 100 : 0, IsUp = current >= 0 };

        var trend = ((current - previous) / previous) * 100;
        return new TrendValueDto
        {
            Value = current,
            Trend = Math.Round(Math.Abs(trend), 2),
            IsUp = trend >= 0
        };
    }
}