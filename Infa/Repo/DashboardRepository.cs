using Core.DTO.Dashboard;
using Core.Entity;
using Core.Interface.Repo;
using Infa.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Infa.Repo;

public class DashboardRepository : IDashboardRepository
{
    private readonly RestaurantMgmtContext _context;

    public DashboardRepository(RestaurantMgmtContext context)
    {
        _context = context;
    }

    public async Task<(int OrderCount, decimal TotalSales, int ReservationCount)> GetSummaryDataAsync(
        DateTime startDate, DateTime endDate, uint completedOrderStatusId, uint completedReservationStatusId, CancellationToken ct = default)
    {
        var orders = await _context.Orders
            .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate && o.OrderStatusLvId == completedOrderStatusId)
            .Select(o => o.TotalAmount)
            .ToListAsync(ct);

        var reservationCount = await _context.Reservations
            .Where(r => r.ReservedTime >= startDate && r.ReservedTime <= endDate && r.ReservationStatusLvId == completedReservationStatusId)
            .CountAsync(ct);

        return (orders.Count, orders.Sum(), reservationCount);
    }

    public async Task<List<RevenueChartItemDto>> GetRevenueChartAsync(
    DateTime startDate, DateTime endDate, uint completedOrderStatusId, CancellationToken ct = default)
    {
        var groupedData = await _context.Orders
            .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate && o.OrderStatusLvId == completedOrderStatusId)
            .GroupBy(o => o.CreatedAt.Value.Date) 
            .Select(g => new
            {
                DateValue = g.Key, 
                Revenue = g.Sum(o => o.TotalAmount), 
                Orders = g.Count() 
            })
            .OrderBy(x => x.DateValue)
            .ToListAsync(ct); 

        return groupedData.Select(x => new RevenueChartItemDto
        {
            Date = x.DateValue.ToString("yyyy-MM-dd"), 
            Revenue = x.Revenue,
            Orders = x.Orders
        }).ToList();
    }


    public async Task<List<TopSellingItemDto>> GetTopSellingAsync(
        DateTime startDate, DateTime endDate, uint completedOrderStatusId, uint imageMediaTypeId, int limit, CancellationToken ct = default)
    {
        var topDishes = await _context.OrderItems
            .Include(oi => oi.Order)
            .Include(oi => oi.Dish)
                .ThenInclude(d => d.DishMedia)
            .Where(oi => oi.Order.CreatedAt >= startDate && oi.Order.CreatedAt <= endDate
                      && oi.Order.OrderStatusLvId == completedOrderStatusId)
            .GroupBy(oi => new { oi.DishId, oi.Dish.DishName })
            .Select(g => new
            {
                DishId = g.Key.DishId,
                DishName = g.Key.DishName,
                TotalQuantity = g.Sum(oi => oi.Quantity),
                ImageUrl = g.FirstOrDefault().Dish.DishMedia
                            .Where(m => m.MediaId == imageMediaTypeId)
                            .Select(m => m.Media.Url)
                            .FirstOrDefault()
            })
            .OrderByDescending(x => x.TotalQuantity)
            .Take(limit)
            .ToListAsync(ct);

        return topDishes.Select(x => new TopSellingItemDto
        {
            DishId = x.DishId,
            DishName = x.DishName,
            TotalQuantity = x.TotalQuantity,
            ImageUrl = x.ImageUrl
        }).ToList();
    }

    public async Task<DashboardStatisticsDto> GetStatisticsAsync(
        DateTime startDate, DateTime endDate, uint completedOrderStatusId, CancellationToken ct = default)
    {
        
        var ordersByType = await _context.Orders
            .Include(o => o.SourceLv)
            .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate)
            .GroupBy(o => o.SourceLv.ValueCode)
            .Select(g => new { Source = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var topCustomer = await _context.Orders
            .Include(o => o.Customer)
            .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate
                     && o.OrderStatusLvId == completedOrderStatusId
                     && o.Customer.FullName != null)
            .GroupBy(o => new { o.CustomerId, o.Customer.FullName })
            .Select(g => new TopCustomerDto
            {
                CustomerId = g.Key.CustomerId,
                CustomerName = g.Key.FullName ?? "Unknown",
                Spent = g.Sum(o => o.TotalAmount)
            })
            .OrderByDescending(x => x.Spent)
            .FirstOrDefaultAsync(ct);

        return new DashboardStatisticsDto
        {
            OrdersByType = ordersByType.ToDictionary(x => x.Source, x => x.Count),
            TopCustomer = topCustomer
        };
    }
}