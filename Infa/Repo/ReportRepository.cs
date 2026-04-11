using Core.DTO.Order;
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

        public async Task<List<TopCustomerDto>> GetDashboardTopSpendersAsync(DateTime startDate, DateTime endDate, uint completedStatusId, CancellationToken ct = default)
        {
            var data = await _context.Orders
                .Where(o => o.CreatedAt >= startDate 
                && o.CreatedAt <= endDate 
                && o.OrderStatusLvId == completedStatusId
                && o.Customer.FullName != "Guest")
                .GroupBy(o => new { o.CustomerId, o.Customer.FullName, o.Customer.Phone })
                .Select(g => new TopCustomerDto
                {
                    CustomerId = g.Key.CustomerId,
                    CustomerName = string.IsNullOrEmpty(g.Key.FullName) ? "Guest" : g.Key.FullName,
                    Phone = g.Key.Phone,
                    TotalOrders = g.Count(),
                    TotalSpent = g.Sum(o => o.TotalAmount),
                    LastVisitDate = g.Max(o => o.CreatedAt)
                })
                .OrderByDescending(x => x.TotalSpent)
                .Take(5) 
                .ToListAsync(ct);

            return data;
        }

        // 1. EARNING DRAWER
        public async Task<DailyEarningDetailDto> GetDailyEarningDetailAsync(DateTime targetDate, uint completedStatusId, CancellationToken ct = default)
        {
            var startOfDay = targetDate.Date;
            var endOfDay = startOfDay.AddDays(1).AddTicks(-1);

            var ordersQuery = _context.Orders
                .Where(o => o.CreatedAt >= startOfDay && o.CreatedAt <= endOfDay && o.OrderStatusLvId == completedStatusId);

            var kpis = await ordersQuery
                .GroupBy(o => 1)
                .Select(g => new
                {
                    TotalNet = g.Sum(o => o.TotalAmount),
                    TotalOrders = g.Count()
                })
                .FirstOrDefaultAsync(ct);

            if (kpis == null || kpis.TotalOrders == 0) return new DailyEarningDetailDto();

            // Group theo giờ 
            var hourlyDataRaw = await ordersQuery
                .GroupBy(o => o.CreatedAt.Value.Hour)
                .Select(g => new { Hour = g.Key, Revenue = g.Sum(o => o.TotalAmount) })
                .ToListAsync(ct);

            // Format lại
            var hourlyRevenue = hourlyDataRaw.Select(h => new HourlyRevenueDto
            {
                Time = $"{h.Hour:D2}:00",
                Revenue = h.Revenue
            }).OrderBy(x => x.Time).ToList();

            // Lấy 5 đơn mới nhất của ngày đó
            var recentOrders = await ordersQuery
                .OrderByDescending(o => o.CreatedAt)
                .Take(5)
                .Select(o => new RecentOrderDto
                {
                    Id = $"#{o.OrderId}",
                    Time = o.CreatedAt.Value.ToString("HH:mm"),
                    Customer = string.IsNullOrEmpty(o.Customer.FullName) ? "Guest" : o.Customer.FullName,
                    Amount = o.TotalAmount,
                    Status = o.OrderStatusLv.ValueName
                })
                .ToListAsync(ct);

            return new DailyEarningDetailDto
            {
                TotalNet = kpis.TotalNet,
                TotalOrders = kpis.TotalOrders,
                AvgOrder = kpis.TotalNet / kpis.TotalOrders,
                HourlyRevenue = hourlyRevenue,
                RecentOrders = recentOrders
            };
        }

        // 2. SALES DRAWER 
        public async Task<DishPerformanceDetailDto> GetDishPerformanceDetailAsync(long dishId, DateTime startDate, DateTime endDate, uint completedStatusId, CancellationToken ct = default)
        {
            var targetItemsQuery = _context.OrderItems
                .Where(oi => oi.DishId == dishId && oi.Order.CreatedAt >= startDate && oi.Order.CreatedAt <= endDate && oi.Order.OrderStatusLvId == completedStatusId);

            var kpis = await targetItemsQuery
                .GroupBy(oi => 1)
                .Select(g => new
                {
                    TotalSold = g.Sum(oi => oi.Quantity),
                    TotalRevenue = g.Sum(oi => oi.Quantity * oi.Price)
                }).FirstOrDefaultAsync(ct);

            if (kpis == null || kpis.TotalSold == 0) return new DishPerformanceDetailDto();

            int totalDays = (endDate - startDate).Days > 0 ? (endDate - startDate).Days : 1;

            var trendDataRaw = await targetItemsQuery
                .GroupBy(oi => oi.Order.CreatedAt.Value.Date)
                .Select(g => new { DateValue = g.Key, Quantity = g.Sum(oi => oi.Quantity) })
                .ToListAsync(ct);

            var trend = trendDataRaw.Select(t => new SalesTrendDto
            {
                Date = t.DateValue.ToString("MM/dd"),
                Quantity = t.Quantity
            }).OrderBy(x => x.Date).ToList();

            var relatedOrderIdsQuery = targetItemsQuery.Select(oi => oi.OrderId).Distinct();
            var freqBoughtRaw = await _context.OrderItems
                .Where(oi => relatedOrderIdsQuery.Contains(oi.OrderId) && oi.DishId != dishId)
                .GroupBy(oi => new { oi.DishId, oi.Dish.DishName })
                .Select(g => new { DishId = g.Key.DishId, DishName = g.Key.DishName, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .Take(5)
                .ToListAsync(ct);

            var crossSells = freqBoughtRaw.Select(f => new CrossSellItemDto
            {
                Id = f.DishId.ToString(),
                Name = f.DishName,
                Frequency = f.Count
            }).ToList();

            return new DishPerformanceDetailDto
            {
                TotalSold = kpis.TotalSold,
                TotalRevenue = kpis.TotalRevenue,
                AvgDailySold = Math.Round((double)kpis.TotalSold / totalDays, 1),
                Trend = trend,
                FrequentlyBoughtWith = crossSells
            };
        }

        // 3. CUSTOMER DRAWER 
        public async Task<CustomerProfileDetailDto> GetCustomerProfileDetailAsync(long customerId, DateTime startDate, DateTime endDate, uint completedStatusId, CancellationToken ct = default)
        {
            var ordersQuery = _context.Orders
                .Where(o => o.CustomerId == customerId && o.CreatedAt >= startDate && o.CreatedAt <= endDate && o.OrderStatusLvId == completedStatusId);

            var kpis = await ordersQuery
                .GroupBy(o => 1)
                .Select(g => new
                {
                    TotalSpent = g.Sum(o => o.TotalAmount),
                    TotalOrders = g.Count()
                }).FirstOrDefaultAsync(ct);

            if (kpis == null || kpis.TotalOrders == 0) return new CustomerProfileDetailDto();

            // Lấy điểm thưởng của khách
            var points = await _context.Customers
                .Where(c => c.CustomerId == customerId)
                .Select(c => c.LoyaltyPoints ?? 0)
                .FirstOrDefaultAsync(ct);

            // Tính phần trăm mua theo danh mục món ăn (Category)
            var categoriesRaw = await _context.OrderItems
                .Where(oi => oi.Order.CustomerId == customerId && oi.Order.OrderStatusLvId == completedStatusId)
                .GroupBy(oi => oi.Dish.Category.CategoryName)
                .Select(g => new { CategoryName = g.Key, Quantity = g.Sum(oi => oi.Quantity) })
                .ToListAsync(ct);

            var totalItemsBought = categoriesRaw.Sum(c => c.Quantity);

            string[] chartColors = { "#1A3A52", "#C5A059", "#f97316", "#10b981", "#8b5cf6" };
            int colorIndex = 0;

            var preferences = categoriesRaw.OrderByDescending(c => c.Quantity).Select(c => new CategoryPreferenceDto
            {
                Name = c.CategoryName,
                Value = totalItemsBought > 0 ? Math.Round((decimal)c.Quantity / totalItemsBought * 100, 1) : 0,
                Color = chartColors[colorIndex++ % chartColors.Length]
            }).ToList();

            var recentOrders = await ordersQuery
                .OrderByDescending(o => o.CreatedAt)
                .Take(5)
                .Select(o => new RecentOrderDto
                {
                    Id = $"#{o.OrderId}",
                    Time = o.CreatedAt.Value.ToString("yyyy-MM-dd"),  
                    Customer = string.Empty, 
                    Amount = o.TotalAmount,
                    Status = o.OrderStatusLv.ValueName
                })
                .ToListAsync(ct);

            return new CustomerProfileDetailDto
            {
                TotalSpent = kpis.TotalSpent,
                TotalOrders = kpis.TotalOrders,
                AvgOrder = kpis.TotalSpent / kpis.TotalOrders,
                Points = points,
                Preferences = preferences,
                RecentOrders = recentOrders
            };
        }
    }


}