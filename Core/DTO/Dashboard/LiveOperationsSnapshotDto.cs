using System;
using System.Collections.Generic;

namespace Core.DTO.Dashboard;

public class LiveOperationsSnapshotRequest
{
    public DateOnly? BusinessDate { get; set; }
}

public class LiveOperationsSnapshotDto
{
    public DateOnly BusinessDate { get; set; }
    public DateTime SnapshotAt { get; set; }
    public LiveTableSnapshotDto Tables { get; set; } = new();
    public LiveOrderPipelineSnapshotDto Orders { get; set; } = new();
    public LiveRevenueSnapshotDto Revenue { get; set; } = new();
    public LiveTopSellingSnapshotDto TopSelling { get; set; } = new();
}

public class LiveTableSnapshotDto
{
    public int OccupiedTables { get; set; }
    public int OccupiedTablesDelta { get; set; }
    public int TotalTables { get; set; }
    public decimal OccupancyRate { get; set; }
    public decimal OccupancyRateDelta { get; set; }
    public int WaitingQueueCount { get; set; }
    public int WaitingQueueDelta { get; set; }
    public int? AverageTurnoverMinutes { get; set; }
    public int? AverageTurnoverDeltaMinutes { get; set; }
}

public class LiveOrderPipelineSnapshotDto
{
    public int PendingCount { get; set; }
    public int CookingCount { get; set; }
    public int ReadyCount { get; set; }
    public int ServedCount { get; set; }
    public int ActiveCount { get; set; }
    public int ActiveCountDelta { get; set; }
    public int ServedCountDelta { get; set; }
}

public class LiveRevenueSnapshotDto
{
    public decimal Revenue { get; set; }
    public decimal RevenueDelta { get; set; }
    public decimal RevenueDeltaPercent { get; set; }
    public int ClosedBills { get; set; }
    public int ClosedBillsDelta { get; set; }
    public decimal AverageBill { get; set; }
}

public class LiveTopSellingSnapshotDto
{
    public int TotalQuantity { get; set; }
    public int TotalQuantityDelta { get; set; }
    public List<LiveTopSellingItemDto> Items { get; set; } = new();
}

public class LiveTopSellingItemDto
{
    public long DishId { get; set; }
    public string DishName { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public int QuantityDelta { get; set; }
    public int? EstimatedPortionsLeft { get; set; }
    public string StockStatusCode { get; set; } = "UNKNOWN";
}

public class LiveOperationsSnapshotQueryWindow
{
    public DateTime CurrentWindowStart { get; set; }
    public DateTime CurrentWindowEnd { get; set; }
    public DateTime CompareWindowStart { get; set; }
    public DateTime CompareWindowEnd { get; set; }
    public DateTime SnapshotAt { get; set; }
    public DateTime QueueWindowStart { get; set; }
    public DateTime QueueWindowEnd { get; set; }
    public DateTime CompareQueueWindowStart { get; set; }
    public DateTime CompareQueueWindowEnd { get; set; }
}

public class LiveOperationsLookupIds
{
    public uint CompletedOrderStatusId { get; set; }
    public uint CancelledOrderStatusId { get; set; }
    public uint PendingReservationStatusId { get; set; }
    public uint ConfirmedReservationStatusId { get; set; }
    public uint OccupiedTableStatusId { get; set; }
    public uint DineInSourceId { get; set; }
    public uint CreatedItemStatusId { get; set; }
    public uint CookingItemStatusId { get; set; }
    public uint ReadyItemStatusId { get; set; }
    public uint ServedItemStatusId { get; set; }
}