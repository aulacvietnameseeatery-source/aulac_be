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
            TopCustomers = topCustomer
        };
    }

    public async Task<LiveOperationsSnapshotDto> GetLiveOperationsSnapshotAsync(
        LiveOperationsSnapshotQueryWindow window,
        LiveOperationsLookupIds lookupIds,
        CancellationToken ct = default)
    {
        var totalTables = await _context.RestaurantTables
            .AsNoTracking()
            .Where(table => !table.IsDeleted)
            .CountAsync(ct);

        var occupiedTables = await _context.RestaurantTables
            .AsNoTracking()
            .Where(table => !table.IsDeleted && table.TableStatusLvId == lookupIds.OccupiedTableStatusId)
            .CountAsync(ct);

        var waitingQueueCount = await _context.Reservations
            .AsNoTracking()
            .Where(reservation => reservation.ReservedTime >= window.QueueWindowStart
                && reservation.ReservedTime <= window.QueueWindowEnd
                && (reservation.ReservationStatusLvId == lookupIds.PendingReservationStatusId
                    || reservation.ReservationStatusLvId == lookupIds.ConfirmedReservationStatusId))
            .CountAsync(ct);

        var compareWaitingQueueCount = await _context.Reservations
            .AsNoTracking()
            .Where(reservation => reservation.ReservedTime >= window.CompareQueueWindowStart
                && reservation.ReservedTime <= window.CompareQueueWindowEnd
                && (reservation.ReservationStatusLvId == lookupIds.PendingReservationStatusId
                    || reservation.ReservationStatusLvId == lookupIds.ConfirmedReservationStatusId))
            .CountAsync(ct);

        var completedOrders = await _context.Orders
            .AsNoTracking()
            .Where(order => order.CreatedAt != null
                && order.OrderStatusLvId == lookupIds.CompletedOrderStatusId
                && ((order.UpdatedAt != null
                        && order.UpdatedAt >= window.CompareWindowStart
                        && order.UpdatedAt <= window.CurrentWindowEnd)
                    || order.Payments.Any(payment => payment.PaidAt != null
                        && payment.PaidAt >= window.CompareWindowStart
                        && payment.PaidAt <= window.CurrentWindowEnd)))
            .Select(order => new CompletedOrderProjection
            {
                OrderId = order.OrderId,
                CreatedAt = order.CreatedAt!.Value,
                ClosedAt = order.Payments.Max(payment => payment.PaidAt) ?? order.UpdatedAt,
                TotalAmount = order.TotalAmount,
                SourceLvId = order.SourceLvId,
                TableId = order.TableId,
            })
            .ToListAsync(ct);

        var occupancyOrders = await _context.Orders
            .AsNoTracking()
            .Where(order => order.TableId != null
                && order.SourceLvId == lookupIds.DineInSourceId
                && order.CreatedAt != null
                && order.CreatedAt <= window.CurrentWindowEnd)
            .Select(order => new OccupancyOrderProjection
            {
                TableId = order.TableId!.Value,
                CreatedAt = order.CreatedAt!.Value,
                UpdatedAt = order.UpdatedAt,
                LatestPaidAt = order.Payments.Max(payment => payment.PaidAt),
                StatusLvId = order.OrderStatusLvId,
            })
            .ToListAsync(ct);

        var pipelineItems = await _context.OrderItems
            .AsNoTracking()
            .Where(item => item.Order.CreatedAt != null
                && item.Order.CreatedAt >= window.CompareWindowStart
                && item.Order.CreatedAt <= window.CurrentWindowEnd
                && item.Order.OrderStatusLvId != lookupIds.CancelledOrderStatusId)
            .Select(item => new PipelineItemProjection
            {
                CreatedAt = item.Order.CreatedAt!.Value,
                ItemStatusLvId = item.ItemStatusLvId,
                Quantity = item.Quantity,
            })
            .ToListAsync(ct);

        var completedOrderItems = await _context.OrderItems
            .AsNoTracking()
            .Where(item => item.Order.OrderStatusLvId == lookupIds.CompletedOrderStatusId
                && item.Order.CreatedAt != null
                && ((item.Order.UpdatedAt != null
                        && item.Order.UpdatedAt >= window.CompareWindowStart
                        && item.Order.UpdatedAt <= window.CurrentWindowEnd)
                    || item.Order.Payments.Any(payment => payment.PaidAt != null
                        && payment.PaidAt >= window.CompareWindowStart
                        && payment.PaidAt <= window.CurrentWindowEnd)))
            .Select(item => new CompletedOrderItemProjection
            {
                DishId = item.DishId,
                DishName = item.Dish.DishName,
                Quantity = item.Quantity,
                ClosedAt = item.Order.Payments.Max(payment => payment.PaidAt) ?? item.Order.UpdatedAt,
            })
            .ToListAsync(ct);

        var currentClosedOrders = completedOrders
            .Where(order => IsWithinWindow(order.ClosedAt, window.CurrentWindowStart, window.CurrentWindowEnd))
            .ToList();
        var compareClosedOrders = completedOrders
            .Where(order => IsWithinWindow(order.ClosedAt, window.CompareWindowStart, window.CompareWindowEnd))
            .ToList();

        var currentRevenue = currentClosedOrders.Sum(order => order.TotalAmount);
        var compareRevenue = compareClosedOrders.Sum(order => order.TotalAmount);
        var currentClosedBills = currentClosedOrders.Count;
        var compareClosedBills = compareClosedOrders.Count;

        var currentAverageBill = currentClosedBills > 0
            ? Math.Round(currentRevenue / currentClosedBills, 2)
            : 0;

        var currentTurnoverMinutes = CalculateAverageTurnoverMinutes(
            currentClosedOrders,
            lookupIds.DineInSourceId,
            window.CurrentWindowStart,
            window.CurrentWindowEnd);
        var compareTurnoverMinutes = CalculateAverageTurnoverMinutes(
            compareClosedOrders,
            lookupIds.DineInSourceId,
            window.CompareWindowStart,
            window.CompareWindowEnd);

        var compareOccupiedTables = EstimateOccupiedTablesAt(
            occupancyOrders,
            window.CompareWindowEnd,
            lookupIds.CompletedOrderStatusId,
            lookupIds.CancelledOrderStatusId);

        var occupancyRate = totalTables == 0
            ? 0
            : Math.Round((decimal)occupiedTables / totalTables * 100, 1);
        var compareOccupancyRate = totalTables == 0
            ? 0
            : Math.Round((decimal)compareOccupiedTables / totalTables * 100, 1);

        var currentPipeline = AggregatePipeline(
            pipelineItems,
            window.CurrentWindowStart,
            window.CurrentWindowEnd,
            lookupIds);
        var comparePipeline = AggregatePipeline(
            pipelineItems,
            window.CompareWindowStart,
            window.CompareWindowEnd,
            lookupIds);

        var currentTopSelling = completedOrderItems
            .Where(item => IsWithinWindow(item.ClosedAt, window.CurrentWindowStart, window.CurrentWindowEnd))
            .GroupBy(item => new { item.DishId, item.DishName })
            .Select(group => new
            {
                group.Key.DishId,
                group.Key.DishName,
                QuantitySold = group.Sum(item => item.Quantity),
            })
            .OrderByDescending(item => item.QuantitySold)
            .ThenBy(item => item.DishName)
            .Take(5)
            .ToList();

        var compareTopSellingMap = completedOrderItems
            .Where(item => IsWithinWindow(item.ClosedAt, window.CompareWindowStart, window.CompareWindowEnd))
            .GroupBy(item => item.DishId)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.Quantity));

        var topDishIds = currentTopSelling.Select(item => item.DishId).ToList();

        var recipeRows = topDishIds.Count == 0
            ? new List<RecipeProjection>()
            : await _context.Recipes
                .AsNoTracking()
                .Where(recipe => topDishIds.Contains(recipe.DishId))
                .Select(recipe => new RecipeProjection
                {
                    DishId = recipe.DishId,
                    IngredientId = recipe.IngredientId,
                    Quantity = recipe.Quantity,
                })
                .ToListAsync(ct);

        var ingredientIds = recipeRows.Select(recipe => recipe.IngredientId).Distinct().ToList();

        var stocks = ingredientIds.Count == 0
            ? new List<StockProjection>()
            : await _context.CurrentStocks
                .AsNoTracking()
                .Where(stock => ingredientIds.Contains(stock.IngredientId))
                .Select(stock => new StockProjection
                {
                    IngredientId = stock.IngredientId,
                    QuantityOnHand = stock.QuantityOnHand,
                    MinStockLevel = stock.MinStockLevel,
                })
                .ToListAsync(ct);

        var dishStatusRows = topDishIds.Count == 0
            ? new List<DishStatusProjection>()
            : await _context.Dishes
                .AsNoTracking()
                .Where(dish => topDishIds.Contains(dish.DishId))
                .Select(dish => new DishStatusProjection
                {
                    DishId = dish.DishId,
                    StatusCode = dish.DishStatusLv.ValueCode,
                })
                .ToListAsync(ct);

        var stockMap = stocks.ToDictionary(stock => stock.IngredientId);
        var recipesByDish = recipeRows.GroupBy(recipe => recipe.DishId).ToDictionary(group => group.Key, group => group.ToList());
        var dishStatusMap = dishStatusRows.ToDictionary(row => row.DishId, row => row.StatusCode);

        var topSellingItems = currentTopSelling.Select(item =>
        {
            var quantityDelta = item.QuantitySold - compareTopSellingMap.GetValueOrDefault(item.DishId);
            var stockState = ComputeStockState(
                item.DishId,
                recipesByDish,
                stockMap,
                dishStatusMap);

            return new LiveTopSellingItemDto
            {
                DishId = item.DishId,
                DishName = item.DishName,
                QuantitySold = item.QuantitySold,
                QuantityDelta = quantityDelta,
                EstimatedPortionsLeft = stockState.EstimatedPortionsLeft,
                StockStatusCode = stockState.StockStatusCode,
            };
        }).ToList();

        var topSellingQuantity = topSellingItems.Sum(item => item.QuantitySold);
        var compareTopSellingQuantity = topSellingItems.Sum(item => compareTopSellingMap.GetValueOrDefault(item.DishId));

        return new LiveOperationsSnapshotDto
        {
            Tables = new LiveTableSnapshotDto
            {
                OccupiedTables = occupiedTables,
                OccupiedTablesDelta = occupiedTables - compareOccupiedTables,
                TotalTables = totalTables,
                OccupancyRate = occupancyRate,
                OccupancyRateDelta = Math.Round(occupancyRate - compareOccupancyRate, 1),
                WaitingQueueCount = waitingQueueCount,
                WaitingQueueDelta = waitingQueueCount - compareWaitingQueueCount,
                AverageTurnoverMinutes = currentTurnoverMinutes,
                AverageTurnoverDeltaMinutes = currentTurnoverMinutes.HasValue && compareTurnoverMinutes.HasValue
                    ? currentTurnoverMinutes.Value - compareTurnoverMinutes.Value
                    : null,
            },
            Orders = new LiveOrderPipelineSnapshotDto
            {
                PendingCount = currentPipeline.Pending,
                CookingCount = currentPipeline.Cooking,
                ReadyCount = currentPipeline.Ready,
                ServedCount = currentPipeline.Served,
                ActiveCount = currentPipeline.Active,
                ActiveCountDelta = currentPipeline.Active - comparePipeline.Active,
                ServedCountDelta = currentPipeline.Served - comparePipeline.Served,
            },
            Revenue = new LiveRevenueSnapshotDto
            {
                Revenue = currentRevenue,
                RevenueDelta = currentRevenue - compareRevenue,
                RevenueDeltaPercent = CalculateDeltaPercent(currentRevenue, compareRevenue),
                ClosedBills = currentClosedBills,
                ClosedBillsDelta = currentClosedBills - compareClosedBills,
                AverageBill = currentAverageBill,
            },
            TopSelling = new LiveTopSellingSnapshotDto
            {
                TotalQuantity = topSellingQuantity,
                TotalQuantityDelta = topSellingQuantity - compareTopSellingQuantity,
                Items = topSellingItems,
            },
        };
    }

    private static bool IsWithinWindow(DateTime? value, DateTime start, DateTime end)
    {
        return value.HasValue && value.Value >= start && value.Value <= end;
    }

    private static int? CalculateAverageTurnoverMinutes(
        IEnumerable<CompletedOrderProjection> orders,
        uint dineInSourceId,
        DateTime start,
        DateTime end)
    {
        var durations = orders
            .Where(order => order.SourceLvId == dineInSourceId
                && order.TableId != null
                && IsWithinWindow(order.ClosedAt, start, end)
                && order.ClosedAt.HasValue
                && order.ClosedAt.Value >= order.CreatedAt)
            .Select(order => (int)Math.Round((order.ClosedAt!.Value - order.CreatedAt).TotalMinutes))
            .Where(minutes => minutes > 0)
            .ToList();

        if (durations.Count == 0)
        {
            return null;
        }

        return (int)Math.Round(durations.Average());
    }

    private static int EstimateOccupiedTablesAt(
        IEnumerable<OccupancyOrderProjection> orders,
        DateTime snapshotAt,
        uint completedOrderStatusId,
        uint cancelledOrderStatusId)
    {
        return orders
            .Where(order => IsOrderActiveAt(order, snapshotAt, completedOrderStatusId, cancelledOrderStatusId))
            .Select(order => order.TableId)
            .Distinct()
            .Count();
    }

    private static bool IsOrderActiveAt(
        OccupancyOrderProjection order,
        DateTime snapshotAt,
        uint completedOrderStatusId,
        uint cancelledOrderStatusId)
    {
        if (order.CreatedAt > snapshotAt)
        {
            return false;
        }

        if (order.StatusLvId != completedOrderStatusId && order.StatusLvId != cancelledOrderStatusId)
        {
            return true;
        }

        var endedAt = order.LatestPaidAt ?? order.UpdatedAt;
        return endedAt.HasValue && endedAt.Value >= snapshotAt;
    }

    private static (int Pending, int Cooking, int Ready, int Served, int Active) AggregatePipeline(
        IEnumerable<PipelineItemProjection> items,
        DateTime start,
        DateTime end,
        LiveOperationsLookupIds lookupIds)
    {
        var scopedItems = items.Where(item => item.CreatedAt >= start && item.CreatedAt <= end).ToList();

        var pending = scopedItems.Where(item => item.ItemStatusLvId == lookupIds.CreatedItemStatusId).Sum(item => item.Quantity);
        var cooking = scopedItems.Where(item => item.ItemStatusLvId == lookupIds.CookingItemStatusId).Sum(item => item.Quantity);
        var ready = scopedItems.Where(item => item.ItemStatusLvId == lookupIds.ReadyItemStatusId).Sum(item => item.Quantity);
        var served = scopedItems.Where(item => item.ItemStatusLvId == lookupIds.ServedItemStatusId).Sum(item => item.Quantity);

        return (pending, cooking, ready, served, pending + cooking + ready);
    }

    private static decimal CalculateDeltaPercent(decimal current, decimal previous)
    {
        if (previous == 0)
        {
            return current > 0 ? 100 : 0;
        }

        return Math.Round(((current - previous) / previous) * 100, 1);
    }

    private static (int? EstimatedPortionsLeft, string StockStatusCode) ComputeStockState(
        long dishId,
        IReadOnlyDictionary<long, List<RecipeProjection>> recipesByDish,
        IReadOnlyDictionary<long, StockProjection> stockMap,
        IReadOnlyDictionary<long, string> dishStatusMap)
    {
        if (dishStatusMap.TryGetValue(dishId, out var dishStatusCode)
            && string.Equals(dishStatusCode, nameof(DishStatusCode.OUT_OF_STOCK), StringComparison.OrdinalIgnoreCase))
        {
            return (0, "OUT");
        }

        if (!recipesByDish.TryGetValue(dishId, out var recipes) || recipes.Count == 0)
        {
            return (null, "UNKNOWN");
        }

        decimal? minPortions = null;
        var isLowStock = false;

        foreach (var recipe in recipes)
        {
            if (recipe.Quantity <= 0 || !stockMap.TryGetValue(recipe.IngredientId, out var stock))
            {
                return (null, "UNKNOWN");
            }

            var portions = Math.Floor(stock.QuantityOnHand / recipe.Quantity);
            minPortions = minPortions.HasValue
                ? Math.Min(minPortions.Value, portions)
                : portions;

            if (stock.QuantityOnHand <= stock.MinStockLevel)
            {
                isLowStock = true;
            }
        }

        if (!minPortions.HasValue)
        {
            return (null, "UNKNOWN");
        }

        var estimatedPortionsLeft = (int)Math.Max(minPortions.Value, 0);

        if (estimatedPortionsLeft <= 0)
        {
            return (0, "OUT");
        }

        if (estimatedPortionsLeft <= 5 || isLowStock)
        {
            return (estimatedPortionsLeft, "LOW");
        }

        return (estimatedPortionsLeft, "HEALTHY");
    }

    private sealed class CompletedOrderProjection
    {
        public long OrderId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public decimal TotalAmount { get; set; }
        public uint SourceLvId { get; set; }
        public long? TableId { get; set; }
    }

    private sealed class OccupancyOrderProjection
    {
        public long TableId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? LatestPaidAt { get; set; }
        public uint StatusLvId { get; set; }
    }

    private sealed class PipelineItemProjection
    {
        public DateTime CreatedAt { get; set; }
        public uint ItemStatusLvId { get; set; }
        public int Quantity { get; set; }
    }

    private sealed class CompletedOrderItemProjection
    {
        public long DishId { get; set; }
        public string DishName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public DateTime? ClosedAt { get; set; }
    }

    private sealed class RecipeProjection
    {
        public long DishId { get; set; }
        public long IngredientId { get; set; }
        public decimal Quantity { get; set; }
    }

    private sealed class StockProjection
    {
        public long IngredientId { get; set; }
        public decimal QuantityOnHand { get; set; }
        public decimal MinStockLevel { get; set; }
    }

    private sealed class DishStatusProjection
    {
        public long DishId { get; set; }
        public string StatusCode { get; set; } = string.Empty;
    }
}