using Core.DTO.Report;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interface.Repo
{
    public interface IReportRepository
    {
        // Earnings
        Task<EarningSummaryDto> GetEarningSummaryAsync(DateTime startDate, DateTime endDate, uint completedStatusId, CancellationToken ct = default);
        Task<List<PaymentMethodRevenueDto>> GetPaymentMethodChartAsync(DateTime startDate, DateTime endDate, uint completedStatusId, CancellationToken ct = default);
        Task<(List<EarningTableItemDto> Data, int TotalCount)> GetEarningTableAsync(DateTime startDate, DateTime endDate, uint completedStatusId, int skip, int take, CancellationToken ct = default);

        // Orders
        Task<OrderMetricsDto> GetOrderMetricsAsync(DateTime startDate, DateTime endDate, uint completedStatusId, uint cancelledStatusId, CancellationToken ct = default);

        // Sales (Items)
        Task<(List<SalesItemDto> Data, int TotalCount)> GetSalesItemsAsync(DateTime startDate, DateTime endDate, uint completedStatusId, int skip, int take, CancellationToken ct = default);

        // Customers
        Task<(List<TopCustomerDto> Data, int TotalCount)> GetTopSpendersAsync(DateTime startDate, DateTime endDate, uint completedStatusId, int skip, int take, CancellationToken ct = default);
    }
}
