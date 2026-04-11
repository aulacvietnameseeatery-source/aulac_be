using Core.DTO.Report;

namespace Core.Interface.Service.Report;

public interface IReportService
{
    Task<EarningSummaryDto> GetEarningSummaryAsync(ReportFilterRequest request, CancellationToken ct = default);
    Task<List<PaymentMethodRevenueDto>> GetPaymentMethodChartAsync(ReportFilterRequest request, CancellationToken ct = default);

    Task<(List<EarningTableItemDto> Items, int TotalCount)> GetEarningTableAsync(ReportFilterRequest request, int pageIndex, int pageSize, CancellationToken ct = default);

    Task<OrderMetricsDto> GetOrderMetricsAsync(ReportFilterRequest request, CancellationToken ct = default);

    Task<(List<SalesItemDto> Items, int TotalCount)> GetSalesItemsAsync(ReportFilterRequest request, int pageIndex, int pageSize, CancellationToken ct = default);

    Task<(List<TopCustomerDto> Items, int TotalCount)> GetTopSpendersAsync(ReportFilterRequest request, int pageIndex, int pageSize, CancellationToken ct = default);

    Task<List<TopCustomerDto>> GetTop5SpendersAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default);

    Task<DailyEarningDetailDto> GetDailyEarningDetailAsync(DateTime date, CancellationToken ct = default);
    Task<DishPerformanceDetailDto> GetDishPerformanceDetailAsync(long dishId, ReportFilterRequest request, CancellationToken ct = default);
    Task<CustomerProfileDetailDto> GetCustomerProfileDetailAsync(long customerId, ReportFilterRequest request, CancellationToken ct = default);
}