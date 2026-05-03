using Core.DTO.Dashboard;
using Core.Enum;
using Core.Interface.Repo;
using Core.Interface.Service.Entity;
using Core.Service;
using FluentAssertions;
using Moq;
using LookupTypeEnum = Core.Enum.LookupType;

namespace Tests.Services;

/// <summary>
/// Unit Test — DashboardService
/// Code Module : Core/Service/DashboardService.cs
/// Methods     : GetSummaryAsync, GetRevenueChartAsync, GetTopSellingAsync, GetStatisticsAsync
/// </summary>
public class DashboardServiceTests
{
    private readonly Mock<IDashboardRepository> _dashRepoMock = new();
    private readonly Mock<ILookupResolver> _lookupMock = new();

    // Lookup ID constants used across tests
    private const uint CompletedOrderStatusId = 500u;
    private const uint CompletedReservationStatusId = 600u;
    private const uint ImageMediaTypeId = 700u;

    private DashboardService CreateService() => new(
        _dashRepoMock.Object,
        _lookupMock.Object);

    /// <summary>
    /// Sets up lookup resolver mocks that all methods need.
    /// All three lookups use the GetIdAsync(ushort, Enum, ct) overload.
    /// </summary>
    private void SetupLookups()
    {
        // OrderStatusCode.COMPLETED → via ToOrderStatusIdAsync extension
        _lookupMock
            .Setup(r => r.GetIdAsync(
                (ushort)LookupTypeEnum.OrderStatus,
                It.IsAny<System.Enum>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CompletedOrderStatusId);

        // ReservationStatusCode.COMPLETED — called directly with enum
        _lookupMock
            .Setup(r => r.GetIdAsync(
                (ushort)LookupTypeEnum.ReservationStatus,
                It.IsAny<System.Enum>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CompletedReservationStatusId);

        // MediaTypeCode.IMAGE — called directly with enum
        _lookupMock
            .Setup(r => r.GetIdAsync(
                (ushort)LookupTypeEnum.MediaType,
                It.IsAny<System.Enum>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ImageMediaTypeId);
    }

    // ──────────────────────────────────────────────────────────────
    #region GetSummaryAsync
    // ──────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetSummaryAsync")]
    public async Task GetSummaryAsync_WithExplicitDates_ReturnsCorrectTrends()
    {
        // Arrange
        SetupLookups();
        var start = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc);
        var request = new DashboardFilterRequest { StartDate = start, EndDate = end };

        // Current period: 10 orders, $5000 total, 8 reservations
        _dashRepoMock
            .Setup(r => r.GetSummaryDataAsync(
                start, end,
                CompletedOrderStatusId, CompletedReservationStatusId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((10, 5000m, 8));

        // Previous period (Mar 23 → Apr 1): 5 orders, $2000, 4 reservations
        var prevStart = new DateTime(2026, 3, 23, 0, 0, 0, DateTimeKind.Utc);
        _dashRepoMock
            .Setup(r => r.GetSummaryDataAsync(
                prevStart, start,
                CompletedOrderStatusId, CompletedReservationStatusId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((5, 2000m, 4));

        var svc = CreateService();

        // Act
        var result = await svc.GetSummaryAsync(request);

        // Assert
        // Orders: 10 vs 5 → +100%
        result.TotalOrders.Value.Should().Be(10);
        result.TotalOrders.Trend.Should().Be(100m);
        result.TotalOrders.IsUp.Should().BeTrue();

        // Sales: 5000 vs 2000 → +150%
        result.TotalSales.Value.Should().Be(5000m);
        result.TotalSales.Trend.Should().Be(150m);
        result.TotalSales.IsUp.Should().BeTrue();

        // AvgOrder: 500 vs 400 → +25%
        result.AverageOrderValue.Value.Should().Be(500m);
        result.AverageOrderValue.Trend.Should().Be(25m);
        result.AverageOrderValue.IsUp.Should().BeTrue();

        // Reservations: 8 vs 4 → +100%
        result.TotalReservations.Value.Should().Be(8);
        result.TotalReservations.Trend.Should().Be(100m);
        result.TotalReservations.IsUp.Should().BeTrue();
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetSummaryAsync")]
    public async Task GetSummaryAsync_WhenCurrentDeclines_ReturnsTrendDown()
    {
        // Arrange
        SetupLookups();
        var start = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 4, 8, 0, 0, 0, DateTimeKind.Utc);
        var request = new DashboardFilterRequest { StartDate = start, EndDate = end };

        // Current: 3 orders, $600, 2 reservations
        _dashRepoMock
            .Setup(r => r.GetSummaryDataAsync(
                start, end,
                CompletedOrderStatusId, CompletedReservationStatusId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((3, 600m, 2));

        // Previous: 10 orders, $2000, 5 reservations
        var prevStart = new DateTime(2026, 3, 25, 0, 0, 0, DateTimeKind.Utc);
        _dashRepoMock
            .Setup(r => r.GetSummaryDataAsync(
                prevStart, start,
                CompletedOrderStatusId, CompletedReservationStatusId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((10, 2000m, 5));

        var svc = CreateService();

        // Act
        var result = await svc.GetSummaryAsync(request);

        // Assert — decline scenario
        // Orders: 3 vs 10 → -70%, IsUp=false
        result.TotalOrders.Value.Should().Be(3);
        result.TotalOrders.Trend.Should().Be(70m);
        result.TotalOrders.IsUp.Should().BeFalse();

        // Sales: 600 vs 2000 → -70%
        result.TotalSales.Value.Should().Be(600m);
        result.TotalSales.Trend.Should().Be(70m);
        result.TotalSales.IsUp.Should().BeFalse();

        // Reservations: 2 vs 5 → -60%
        result.TotalReservations.Value.Should().Be(2);
        result.TotalReservations.Trend.Should().Be(60m);
        result.TotalReservations.IsUp.Should().BeFalse();
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetSummaryAsync")]
    public async Task GetSummaryAsync_WhenPreviousPeriodIsZero_ReturnsTrend100()
    {
        // Arrange
        SetupLookups();
        var start = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 4, 8, 0, 0, 0, DateTimeKind.Utc);
        var request = new DashboardFilterRequest { StartDate = start, EndDate = end };

        // Current: 5 orders, $1000, 3 reservations
        _dashRepoMock
            .Setup(r => r.GetSummaryDataAsync(
                start, end,
                CompletedOrderStatusId, CompletedReservationStatusId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((5, 1000m, 3));

        // Previous: 0, 0, 0 — triggers previous==0 branch in CalculateTrend
        var prevStart = new DateTime(2026, 3, 25, 0, 0, 0, DateTimeKind.Utc);
        _dashRepoMock
            .Setup(r => r.GetSummaryDataAsync(
                prevStart, start,
                CompletedOrderStatusId, CompletedReservationStatusId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((0, 0m, 0));

        var svc = CreateService();

        // Act
        var result = await svc.GetSummaryAsync(request);

        // Assert — previous=0: Trend=100 when current>0, IsUp=true
        result.TotalOrders.Value.Should().Be(5);
        result.TotalOrders.Trend.Should().Be(100m);
        result.TotalOrders.IsUp.Should().BeTrue();

        result.TotalSales.Value.Should().Be(1000m);
        result.TotalSales.Trend.Should().Be(100m);
        result.TotalSales.IsUp.Should().BeTrue();
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetSummaryAsync")]
    public async Task GetSummaryAsync_WhenBothPeriodsZero_ReturnsTrendZero()
    {
        // Arrange
        SetupLookups();
        var start = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 4, 8, 0, 0, 0, DateTimeKind.Utc);
        var request = new DashboardFilterRequest { StartDate = start, EndDate = end };

        // Both periods: 0, 0, 0
        _dashRepoMock
            .Setup(r => r.GetSummaryDataAsync(
                It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                CompletedOrderStatusId, CompletedReservationStatusId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((0, 0m, 0));

        var svc = CreateService();

        // Act
        var result = await svc.GetSummaryAsync(request);

        // Assert — both zero: Trend=0, IsUp=true (0 >= 0)
        result.TotalOrders.Value.Should().Be(0);
        result.TotalOrders.Trend.Should().Be(0m);
        result.TotalOrders.IsUp.Should().BeTrue();

        result.TotalSales.Value.Should().Be(0m);
        result.TotalSales.Trend.Should().Be(0m);
        result.TotalSales.IsUp.Should().BeTrue();

        // Avg = 0 because OrderCount=0
        result.AverageOrderValue.Value.Should().Be(0m);
        result.AverageOrderValue.Trend.Should().Be(0m);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetSummaryAsync")]
    public async Task GetSummaryAsync_WhenDatesAreNull_UsesDefaultSevenDayRange()
    {
        // Arrange
        SetupLookups();
        var request = new DashboardFilterRequest { StartDate = null, EndDate = null };

        // Match any date range — we just verify the method executes without error
        _dashRepoMock
            .Setup(r => r.GetSummaryDataAsync(
                It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                CompletedOrderStatusId, CompletedReservationStatusId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((1, 100m, 1));

        var svc = CreateService();

        // Act
        var result = await svc.GetSummaryAsync(request);

        // Assert — should not throw, returns valid summary
        result.Should().NotBeNull();
        result.TotalOrders.Value.Should().Be(1);
        result.TotalSales.Value.Should().Be(100m);

        // Verify GetSummaryDataAsync called exactly twice (current + previous)
        _dashRepoMock.Verify(r => r.GetSummaryDataAsync(
            It.IsAny<DateTime>(), It.IsAny<DateTime>(),
            CompletedOrderStatusId, CompletedReservationStatusId,
            It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetSummaryAsync")]
    public async Task GetSummaryAsync_TrendRounding_RoundsToTwoDecimals()
    {
        // Arrange
        SetupLookups();
        var start = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc);
        var request = new DashboardFilterRequest { StartDate = start, EndDate = end };

        // Current: 7 orders, $3333.33
        _dashRepoMock
            .Setup(r => r.GetSummaryDataAsync(
                start, end,
                CompletedOrderStatusId, CompletedReservationStatusId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((7, 3333.33m, 3));

        // Previous: 3 orders, $1000
        var prevStart = new DateTime(2026, 3, 23, 0, 0, 0, DateTimeKind.Utc);
        _dashRepoMock
            .Setup(r => r.GetSummaryDataAsync(
                prevStart, start,
                CompletedOrderStatusId, CompletedReservationStatusId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((3, 1000m, 1));

        var svc = CreateService();

        // Act
        var result = await svc.GetSummaryAsync(request);

        // Assert — Orders: (7-3)/3*100 = 133.33...% → rounded to 133.33
        result.TotalOrders.Trend.Should().Be(133.33m);

        // Sales: (3333.33-1000)/1000*100 = 233.333% → rounded to 233.33
        result.TotalSales.Trend.Should().Be(233.33m);
    }

    #endregion

    // ──────────────────────────────────────────────────────────────
    #region GetRevenueChartAsync
    // ──────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetRevenueChartAsync")]
    public async Task GetRevenueChartAsync_WithExplicitDates_ReturnsChartData()
    {
        // Arrange
        SetupLookups();
        var start = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 4, 7, 0, 0, 0, DateTimeKind.Utc);
        var request = new DashboardFilterRequest { StartDate = start, EndDate = end };

        var chartData = new List<RevenueChartItemDto>
        {
            new() { Date = "2026-04-01", Revenue = 1200m, Orders = 5 },
            new() { Date = "2026-04-02", Revenue = 800m, Orders = 3 },
            new() { Date = "2026-04-03", Revenue = 1500m, Orders = 7 }
        };
        _dashRepoMock
            .Setup(r => r.GetRevenueChartAsync(
                start, end, CompletedOrderStatusId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(chartData);

        var svc = CreateService();

        // Act
        var result = await svc.GetRevenueChartAsync(request);

        // Assert
        result.Should().HaveCount(3);
        result[0].Date.Should().Be("2026-04-01");
        result[0].Revenue.Should().Be(1200m);
        result[0].Orders.Should().Be(5);
        result[1].Date.Should().Be("2026-04-02");
        result[2].Revenue.Should().Be(1500m);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetRevenueChartAsync")]
    public async Task GetRevenueChartAsync_WithNullDates_UsesDefaultSevenDayRange()
    {
        // Arrange
        SetupLookups();
        var request = new DashboardFilterRequest { StartDate = null, EndDate = null };

        var chartData = new List<RevenueChartItemDto>
        {
            new() { Date = "2026-04-13", Revenue = 500m, Orders = 2 }
        };
        _dashRepoMock
            .Setup(r => r.GetRevenueChartAsync(
                It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                CompletedOrderStatusId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(chartData);

        var svc = CreateService();

        // Act
        var result = await svc.GetRevenueChartAsync(request);

        // Assert
        result.Should().HaveCount(1);
        result[0].Revenue.Should().Be(500m);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetRevenueChartAsync")]
    public async Task GetRevenueChartAsync_WhenNoData_ReturnsEmptyList()
    {
        // Arrange
        SetupLookups();
        var start = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2020, 1, 7, 0, 0, 0, DateTimeKind.Utc);
        var request = new DashboardFilterRequest { StartDate = start, EndDate = end };

        _dashRepoMock
            .Setup(r => r.GetRevenueChartAsync(
                start, end, CompletedOrderStatusId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RevenueChartItemDto>());

        var svc = CreateService();

        // Act
        var result = await svc.GetRevenueChartAsync(request);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    // ──────────────────────────────────────────────────────────────
    #region GetTopSellingAsync
    // ──────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetTopSellingAsync")]
    public async Task GetTopSellingAsync_WithExplicitDatesAndDefaultLimit_ReturnsTopItems()
    {
        // Arrange
        SetupLookups();
        var start = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 3, 31, 0, 0, 0, DateTimeKind.Utc);
        var request = new DashboardFilterRequest { StartDate = start, EndDate = end };

        var topItems = new List<TopSellingItemDto>
        {
            new() { DishId = 1, DishName = "Pho Bo", TotalQuantity = 120, ImageUrl = "/images/pho.jpg" },
            new() { DishId = 2, DishName = "Bun Cha", TotalQuantity = 95, ImageUrl = "/images/buncha.jpg" },
            new() { DishId = 3, DishName = "Com Tam", TotalQuantity = 80, ImageUrl = null }
        };
        _dashRepoMock
            .Setup(r => r.GetTopSellingAsync(
                start, end, CompletedOrderStatusId, ImageMediaTypeId,
                6, It.IsAny<CancellationToken>()))
            .ReturnsAsync(topItems);

        var svc = CreateService();

        // Act — default limit=6
        var result = await svc.GetTopSellingAsync(request);

        // Assert
        result.Should().HaveCount(3);
        result[0].DishId.Should().Be(1);
        result[0].DishName.Should().Be("Pho Bo");
        result[0].TotalQuantity.Should().Be(120);
        result[0].ImageUrl.Should().Be("/images/pho.jpg");
        result[2].ImageUrl.Should().BeNull();
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetTopSellingAsync")]
    public async Task GetTopSellingAsync_WithCustomLimit_PassesLimitToRepo()
    {
        // Arrange
        SetupLookups();
        var start = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 3, 31, 0, 0, 0, DateTimeKind.Utc);
        var request = new DashboardFilterRequest { StartDate = start, EndDate = end };

        var topItems = new List<TopSellingItemDto>
        {
            new() { DishId = 10, DishName = "Spring Rolls", TotalQuantity = 200, ImageUrl = "/img/roll.jpg" }
        };
        _dashRepoMock
            .Setup(r => r.GetTopSellingAsync(
                start, end, CompletedOrderStatusId, ImageMediaTypeId,
                3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(topItems);

        var svc = CreateService();

        // Act — custom limit=3
        var result = await svc.GetTopSellingAsync(request, limit: 3);

        // Assert
        result.Should().HaveCount(1);
        result[0].DishName.Should().Be("Spring Rolls");
        result[0].TotalQuantity.Should().Be(200);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetTopSellingAsync")]
    public async Task GetTopSellingAsync_WithNullDates_UsesDefaultThirtyDayRange()
    {
        // Arrange
        SetupLookups();
        var request = new DashboardFilterRequest { StartDate = null, EndDate = null };

        _dashRepoMock
            .Setup(r => r.GetTopSellingAsync(
                It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                CompletedOrderStatusId, ImageMediaTypeId,
                6, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TopSellingItemDto>());

        var svc = CreateService();

        // Act
        var result = await svc.GetTopSellingAsync(request);

        // Assert
        result.Should().BeEmpty();
        _dashRepoMock.Verify(r => r.GetTopSellingAsync(
            It.IsAny<DateTime>(), It.IsAny<DateTime>(),
            CompletedOrderStatusId, ImageMediaTypeId,
            6, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetTopSellingAsync")]
    public async Task GetTopSellingAsync_LimitOne_ReturnsOnlyTopDish()
    {
        // Arrange
        SetupLookups();
        var start = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 4, 30, 0, 0, 0, DateTimeKind.Utc);
        var request = new DashboardFilterRequest { StartDate = start, EndDate = end };

        var topItems = new List<TopSellingItemDto>
        {
            new() { DishId = 5, DishName = "Banh Mi", TotalQuantity = 300, ImageUrl = "/img/banhmi.jpg" }
        };
        _dashRepoMock
            .Setup(r => r.GetTopSellingAsync(
                start, end, CompletedOrderStatusId, ImageMediaTypeId,
                1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(topItems);

        var svc = CreateService();

        // Act
        var result = await svc.GetTopSellingAsync(request, limit: 1);

        // Assert
        result.Should().HaveCount(1);
        result[0].DishName.Should().Be("Banh Mi");
        result[0].TotalQuantity.Should().Be(300);
    }

    #endregion

    // ──────────────────────────────────────────────────────────────
    #region GetStatisticsAsync
    // ──────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetStatisticsAsync")]
    public async Task GetStatisticsAsync_WithExplicitDates_ReturnsStatistics()
    {
        // Arrange
        SetupLookups();
        var start = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 3, 31, 0, 0, 0, DateTimeKind.Utc);
        var request = new DashboardFilterRequest { StartDate = start, EndDate = end };

        var stats = new DashboardStatisticsDto
        {
            OrdersByType = new Dictionary<string, int>
            {
                ["DINE_IN"] = 50,
                ["TAKE_AWAY"] = 30,
                ["QR_ORDER"] = 20
            },
            TopCustomers = new TopCustomerDto
            {
                CustomerId = 42,
                CustomerName = "Nguyen Van A",
                Spent = 5000m
            }
        };
        _dashRepoMock
            .Setup(r => r.GetStatisticsAsync(
                start, end, CompletedOrderStatusId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stats);

        var svc = CreateService();

        // Act
        var result = await svc.GetStatisticsAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.OrdersByType.Should().HaveCount(3);
        result.OrdersByType["DINE_IN"].Should().Be(50);
        result.OrdersByType["TAKE_AWAY"].Should().Be(30);
        result.OrdersByType["QR_ORDER"].Should().Be(20);
        result.TopCustomers.Should().NotBeNull();
        result.TopCustomers!.CustomerId.Should().Be(42);
        result.TopCustomers.CustomerName.Should().Be("Nguyen Van A");
        result.TopCustomers.Spent.Should().Be(5000m);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetStatisticsAsync")]
    public async Task GetStatisticsAsync_WithNullDates_UsesDefaultThirtyDayRange()
    {
        // Arrange
        SetupLookups();
        var request = new DashboardFilterRequest { StartDate = null, EndDate = null };

        var stats = new DashboardStatisticsDto
        {
            OrdersByType = new Dictionary<string, int> { ["DINE_IN"] = 10 },
            TopCustomers = null
        };
        _dashRepoMock
            .Setup(r => r.GetStatisticsAsync(
                It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                CompletedOrderStatusId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stats);

        var svc = CreateService();

        // Act
        var result = await svc.GetStatisticsAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.OrdersByType.Should().ContainKey("DINE_IN");
        result.TopCustomers.Should().BeNull();
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetStatisticsAsync")]
    public async Task GetStatisticsAsync_WhenNoOrders_ReturnsEmptyDictionary()
    {
        // Arrange
        SetupLookups();
        var start = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2020, 1, 31, 0, 0, 0, DateTimeKind.Utc);
        var request = new DashboardFilterRequest { StartDate = start, EndDate = end };

        var stats = new DashboardStatisticsDto
        {
            OrdersByType = new Dictionary<string, int>(),
            TopCustomers = null
        };
        _dashRepoMock
            .Setup(r => r.GetStatisticsAsync(
                start, end, CompletedOrderStatusId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stats);

        var svc = CreateService();

        // Act
        var result = await svc.GetStatisticsAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.OrdersByType.Should().BeEmpty();
        result.TopCustomers.Should().BeNull();
    }

    #endregion
}
