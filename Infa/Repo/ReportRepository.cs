using Core.DTO.Report;
using Core.Entity;
using Core.Interface.Repo;
using Infa.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Infa.Repository
{
    public class ReportRepository : IReportRepository
    {
        private readonly RestaurantMgmtContext _context;

        public ReportRepository(RestaurantMgmtContext context)
        {
            _context = context;
        }

        public async Task<EarningSummaryDto> GetEarningSummaryAsync(DateTime startDate, DateTime endDate, uint completedStatusId, CancellationToken ct = default)
        {
            var summary = await _context.Orders
                .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate && o.OrderStatusLvId == completedStatusId)
                .GroupBy(o => 1) 
                .Select(g => new EarningSummaryDto
                {
                    GrossRevenue = g.Sum(o => o.SubTotalAmount),
                    NetRevenue = g.Sum(o => o.TotalAmount),
                    TotalTax = g.Sum(o => o.TaxAmount),
                    TotalDiscount = g.Sum(o => (o.SubTotalAmount + o.TaxAmount) - o.TotalAmount)
                })
                .FirstOrDefaultAsync(ct);

            return summary ?? new EarningSummaryDto();
        }

        public async Task<List<PaymentMethodRevenueDto>> GetPaymentMethodChartAsync(DateTime startDate, DateTime endDate, uint completedStatusId, CancellationToken ct = default)
        {
            var data = await _context.Payments
                .Where(p => p.Order.CreatedAt >= startDate && p.Order.CreatedAt <= endDate && p.Order.OrderStatusLvId == completedStatusId)
                .GroupBy(p => new { p.MethodLvId, MethodName = p.MethodLv.ValueName })
                .Select(g => new PaymentMethodRevenueDto
                {
                    MethodName = g.Key.MethodName,
                    Amount = g.Sum(p => p.ReceivedAmount - p.ChangeAmount) 
                })
                .ToListAsync(ct);

            var totalAmount = data.Sum(x => x.Amount);
            if (totalAmount > 0)
            {
                foreach (var item in data)
                {
                    item.Percentage = Math.Round((double)(item.Amount / totalAmount) * 100, 2);
                }
            }

            return data.OrderByDescending(x => x.Amount).ToList();
        }

        public async Task<(List<EarningTableItemDto> Data, int TotalCount)> GetEarningTableAsync(DateTime startDate, DateTime endDate, uint completedStatusId, int skip, int take, CancellationToken ct = default)
        {
            var query = _context.Orders
                .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate && o.OrderStatusLvId == completedStatusId)
                .GroupBy(o => o.CreatedAt.Value.Date) 
                .Select(g => new
                {
                    DateValue = g.Key,
                    TotalOrders = g.Count(),
                    GrossRevenue = g.Sum(o => o.SubTotalAmount),
                    NetRevenue = g.Sum(o => o.TotalAmount),
                    TotalTax = g.Sum(o => o.TaxAmount)
                });

            var totalCount = await query.CountAsync(ct);

            var rawData = await query
                .OrderByDescending(x => x.DateValue)
                .Skip(skip)
                .Take(take)
                .ToListAsync(ct);

            var data = rawData.Select(x => new EarningTableItemDto
            {
                Date = x.DateValue.ToString("yyyy-MM-dd"),
                TotalOrders = x.TotalOrders,
                GrossRevenue = x.GrossRevenue,
                NetRevenue = x.NetRevenue,
                TotalTax = x.TotalTax
            }).ToList();

            return (data, totalCount);
        }

        public async Task<OrderMetricsDto> GetOrderMetricsAsync(DateTime startDate, DateTime endDate, uint completedStatusId, uint cancelledStatusId, CancellationToken ct = default)
        {
            var metrics = await _context.Orders
                .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate)
                .GroupBy(o => 1)
                .Select(g => new
                {
                    Total = g.Count(),
                    Completed = g.Count(o => o.OrderStatusLvId == completedStatusId),
                    Cancelled = g.Count(o => o.OrderStatusLvId == cancelledStatusId)
                })
                .FirstOrDefaultAsync(ct);

            if (metrics == null) return new OrderMetricsDto();

            return new OrderMetricsDto
            {
                TotalOrders = metrics.Total,
                CompletedOrders = metrics.Completed,
                CancelledOrders = metrics.Cancelled,
                CancelRate = metrics.Total > 0 ? Math.Round((double)metrics.Cancelled / metrics.Total * 100, 2) : 0
            };
        }


        public async Task<(List<SalesItemDto> Data, int TotalCount)> GetSalesItemsAsync(DateTime startDate, DateTime endDate, uint completedStatusId, int skip, int take, CancellationToken ct = default)
        {
            var query = _context.OrderItems
                .Where(oi => oi.Order.CreatedAt >= startDate && oi.Order.CreatedAt <= endDate && oi.Order.OrderStatusLvId == completedStatusId)
                .GroupBy(oi => new
                {
                    oi.DishId,
                    DishName = oi.Dish.DishName,
                    CategoryName = oi.Dish.Category.CategoryName
                })
                .Select(g => new SalesItemDto
                {
                    DishId = g.Key.DishId,
                    DishName = g.Key.DishName,
                    CategoryName = g.Key.CategoryName,
                    QuantitySold = g.Sum(oi => oi.Quantity),
                    TotalRevenue = g.Sum(oi => oi.Quantity * oi.Price) 
                });

            var totalCount = await query.CountAsync(ct);

            var data = await query
                .OrderByDescending(x => x.TotalRevenue) 
                .Skip(skip)
                .Take(take)
                .ToListAsync(ct);

            return (data, totalCount);
        }

        public async Task<(List<TopCustomerDto> Data, int TotalCount)> GetTopSpendersAsync(DateTime startDate, DateTime endDate, uint completedStatusId, int skip, int take, CancellationToken ct = default)
        {
            var query = _context.Orders
                .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate && o.OrderStatusLvId == completedStatusId)
                .GroupBy(o => new { o.CustomerId, o.Customer.FullName, o.Customer.Phone })
                .Select(g => new TopCustomerDto
                {
                    CustomerId = g.Key.CustomerId,
                    CustomerName = string.IsNullOrEmpty(g.Key.FullName) ? "Guest" : g.Key.FullName,
                    Phone = g.Key.Phone,
                    TotalOrders = g.Count(),
                    TotalSpent = g.Sum(o => o.TotalAmount),
                    LastVisitDate = g.Max(o => o.CreatedAt)
                });

            var totalCount = await query.CountAsync(ct);

            var data = await query
                .OrderByDescending(x => x.TotalSpent) 
                .Skip(skip)
                .Take(take)
                .ToListAsync(ct);

            return (data, totalCount);
        }
    }
}