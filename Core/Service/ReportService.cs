using Core.DTO.Report;
using Core.Enum;
using Core.Interface.Repo; 
using Core.Interface.Service.Entity; 
using Core.Interface.Service.Report; 
using Microsoft.Extensions.Logging;

namespace Core.Service;

/// <summary>
/// Implementation of IReportService handling report business logic
/// </summary>
public class ReportService : IReportService
{
    private readonly IReportRepository _reportRepository;
    private readonly ILookupResolver _lookupResolver;
    private readonly ILogger<ReportService> _logger;

    public ReportService(
        IReportRepository reportRepository,
        ILookupResolver lookupResolver,
        ILogger<ReportService> logger)
    {
        _reportRepository = reportRepository;
        _lookupResolver = lookupResolver;
        _logger = logger;
    }

    /// <summary>
    /// Helper method to resolve OrderStatus ID safely
    /// </summary>
    private async Task<uint> GetOrderStatusIdAsync(OrderStatusCode statusCode, CancellationToken ct)
    {
        return await _lookupResolver.GetIdAsync((ushort)LookupType.OrderStatus, statusCode, ct);
    }

    // TAB 1: EARNINGS

    public async Task<EarningSummaryDto> GetEarningSummaryAsync(ReportFilterRequest request, CancellationToken ct = default)
    {
        try
        {
            var completedId = await GetOrderStatusIdAsync(OrderStatusCode.COMPLETED, ct);
            return await _reportRepository.GetEarningSummaryAsync(request.StartDate, request.EndDate, completedId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting earning summary for period {Start} to {End}", request.StartDate, request.EndDate);
            throw;
        }
    }

    public async Task<List<PaymentMethodRevenueDto>> GetPaymentMethodChartAsync(ReportFilterRequest request, CancellationToken ct = default)
    {
        try
        {
            var completedId = await GetOrderStatusIdAsync(OrderStatusCode.COMPLETED, ct);
            return await _reportRepository.GetPaymentMethodChartAsync(request.StartDate, request.EndDate, completedId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment method chart");
            throw;
        }
    }

    public async Task<(List<EarningTableItemDto> Items, int TotalCount)> GetEarningTableAsync(ReportFilterRequest request, int pageIndex, int pageSize, CancellationToken ct = default)
    {
        try
        {
            var completedId = await GetOrderStatusIdAsync(OrderStatusCode.COMPLETED, ct);

            int skip = (pageIndex - 1) * pageSize;

            return await _reportRepository.GetEarningTableAsync(request.StartDate, request.EndDate, completedId, skip, pageSize, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting earning table data");
            throw;
        }
    }

    // TAB 2: ORDERS

    public async Task<OrderMetricsDto> GetOrderMetricsAsync(ReportFilterRequest request, CancellationToken ct = default)
    {
        try
        {
            var completedId = await GetOrderStatusIdAsync(OrderStatusCode.COMPLETED, ct);
            var cancelledId = await GetOrderStatusIdAsync(OrderStatusCode.CANCELLED, ct);

            return await _reportRepository.GetOrderMetricsAsync(request.StartDate, request.EndDate, completedId, cancelledId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order metrics");
            throw;
        }
    }

    // TAB 3: SALES

    public async Task<(List<SalesItemDto> Items, int TotalCount)> GetSalesItemsAsync(ReportFilterRequest request, int pageIndex, int pageSize, CancellationToken ct = default)
    {
        try
        {
            var completedId = await GetOrderStatusIdAsync(OrderStatusCode.COMPLETED, ct);

            int skip = (pageIndex - 1) * pageSize;

            return await _reportRepository.GetSalesItemsAsync(request.StartDate, request.EndDate, completedId, skip, pageSize, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sales items for reports");
            throw;
        }
    }

    // TAB 4: CUSTOMERS

    public async Task<(List<TopCustomerDto> Items, int TotalCount)> GetTopSpendersAsync(ReportFilterRequest request, int pageIndex, int pageSize, CancellationToken ct = default)
    {
        try
        {
            var completedId = await GetOrderStatusIdAsync(OrderStatusCode.COMPLETED, ct);

            int skip = (pageIndex - 1) * pageSize;

            return await _reportRepository.GetTopSpendersAsync(request.StartDate, request.EndDate, completedId, skip, pageSize, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting top spender customers");
            throw;
        }
    }

    public async Task<List<TopCustomerDto>> GetTop5SpendersAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default)
    {
        var completedId = await GetOrderStatusIdAsync(OrderStatusCode.COMPLETED, ct);

        var top5Customers = await _reportRepository.GetDashboardTopSpendersAsync(
            startDate,
            endDate,
            completedId,
            ct
        );

        return top5Customers;
    }

}