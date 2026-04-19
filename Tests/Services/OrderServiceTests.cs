using Core.Data;
using Core.DTO.Customer;
using Core.DTO.General;
using Core.DTO.Notification;
using Core.DTO.Order;
using Core.DTO.Shift;
using Core.Entity;
using Core.Enum;
using Core.Exceptions;
using Core.Extensions;
using Core.Interface.Repo;
using Core.Interface.Service.Customer;
using Core.Interface.Service.Entity;
using Core.Interface.Service.Notification;
using Core.Interface.Service.Others;
using Core.Interface.Service.Shift;
using Core.Service;
using FluentAssertions;
using Moq;
using LookupTypeEnum = Core.Enum.LookupType;

namespace Tests.Services;

/// <summary>
/// Unit Test — OrderService
/// Code Module : Core/Service/OrderService.cs
/// Method      : GetOrderHistoryAsync, GetOrderStatusCountAsync, GetKitchenOrdersAsync,
///               UpdateOrderStatusAsync, CancelOrderItemAsync, GetOrderByIdAsync,
///               CreateOrderAsync (Staff), AddItemsAsync, CreateOrderAsync (Customer),
///               AddItemsToOrderAsync, GetRecentOrdersAsync
/// Created By  : quantm
/// Executed By : quantm
/// Test Req.   : Verify order management business logic including order listing,
///               status counting, kitchen view, status transitions, item cancellation,
///               order creation by staff and customer, item additions, and recent orders.
/// </summary>
public class OrderServiceTests
{
    // ── Mocks ──
    private readonly Mock<IOrderRepository> _orderRepoMock = new();
    private readonly Mock<ITableRepository> _tableRepoMock = new();
    private readonly Mock<ILookupResolver> _lookupResolverMock = new();
    private readonly Mock<IDishRepository> _dishRepoMock = new();
    private readonly Mock<ICustomerService> _customerServiceMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<INotificationService> _notificationServiceMock = new();
    private readonly Mock<IOrderRealtimeService> _realtimeMock = new();
    private readonly Mock<IShiftLiveRealtimePublisher> _shiftLiveMock = new();
    private readonly Mock<ITaxRepository> _taxRepoMock = new();

    // ── Lookup IDs ──
    private const uint PendingStatusId = 100;
    private const uint InProgressStatusId = 101;
    private const uint CompletedStatusId = 102;
    private const uint CancelledStatusId = 103;
    private const uint AvailableTableStatusId = 200;
    private const uint OccupiedTableStatusId = 201;
    private const uint ReservedTableStatusId = 202;
    private const uint LockedTableStatusId = 203;
    private const uint CreatedItemStatusId = 300;
    private const uint InProgressItemStatusId = 301;
    private const uint ReadyItemStatusId = 302;
    private const uint ServedItemStatusId = 303;
    private const uint RejectedItemStatusId = 304;
    private const uint CancelledItemStatusId = 305;
    private const uint DineInSourceId = 400;
    private const uint TakeawaySourceId = 401;

    // ── Factory ──
    private OrderService CreateService() => new(
        _orderRepoMock.Object,
        _tableRepoMock.Object,
        _lookupResolverMock.Object,
        _dishRepoMock.Object,
        _customerServiceMock.Object,
        _uowMock.Object,
        _notificationServiceMock.Object,
        _realtimeMock.Object,
        _shiftLiveMock.Object,
        _taxRepoMock.Object);

    // ── Setup Helpers ──
    private void SetupLookupResolver()
    {
        // Order statuses
        _lookupResolverMock
            .Setup(x => x.GetIdAsync((ushort)LookupTypeEnum.OrderStatus, It.Is<System.Enum>(e => e.ToString() == "PENDING"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(PendingStatusId);
        _lookupResolverMock
            .Setup(x => x.GetIdAsync((ushort)LookupTypeEnum.OrderStatus, It.Is<System.Enum>(e => e.ToString() == "IN_PROGRESS"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(InProgressStatusId);
        _lookupResolverMock
            .Setup(x => x.GetIdAsync((ushort)LookupTypeEnum.OrderStatus, It.Is<System.Enum>(e => e.ToString() == "COMPLETED"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CompletedStatusId);
        _lookupResolverMock
            .Setup(x => x.GetIdAsync((ushort)LookupTypeEnum.OrderStatus, It.Is<System.Enum>(e => e.ToString() == "CANCELLED"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CancelledStatusId);

        // Table statuses
        _lookupResolverMock
            .Setup(x => x.GetIdAsync((ushort)LookupTypeEnum.TableStatus, It.Is<System.Enum>(e => e.ToString() == "AVAILABLE"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AvailableTableStatusId);
        _lookupResolverMock
            .Setup(x => x.GetIdAsync((ushort)LookupTypeEnum.TableStatus, It.Is<System.Enum>(e => e.ToString() == "OCCUPIED"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OccupiedTableStatusId);
        _lookupResolverMock
            .Setup(x => x.GetIdAsync((ushort)LookupTypeEnum.TableStatus, It.Is<System.Enum>(e => e.ToString() == "RESERVED"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ReservedTableStatusId);
        _lookupResolverMock
            .Setup(x => x.GetIdAsync((ushort)LookupTypeEnum.TableStatus, It.Is<System.Enum>(e => e.ToString() == "LOCKED"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(LockedTableStatusId);

        // Order item statuses
        _lookupResolverMock
            .Setup(x => x.GetIdAsync((ushort)LookupTypeEnum.OrderItemStatus, It.Is<System.Enum>(e => e.ToString() == "CREATED"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatedItemStatusId);
        _lookupResolverMock
            .Setup(x => x.GetIdAsync((ushort)LookupTypeEnum.OrderItemStatus, It.Is<System.Enum>(e => e.ToString() == "IN_PROGRESS"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(InProgressItemStatusId);
        _lookupResolverMock
            .Setup(x => x.GetIdAsync((ushort)LookupTypeEnum.OrderItemStatus, It.Is<System.Enum>(e => e.ToString() == "READY"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ReadyItemStatusId);
        _lookupResolverMock
            .Setup(x => x.GetIdAsync((ushort)LookupTypeEnum.OrderItemStatus, It.Is<System.Enum>(e => e.ToString() == "SERVED"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServedItemStatusId);
        _lookupResolverMock
            .Setup(x => x.GetIdAsync((ushort)LookupTypeEnum.OrderItemStatus, It.Is<System.Enum>(e => e.ToString() == "REJECTED"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(RejectedItemStatusId);
        _lookupResolverMock
            .Setup(x => x.GetIdAsync((ushort)LookupTypeEnum.OrderItemStatus, It.Is<System.Enum>(e => e.ToString() == "CANCELLED"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CancelledItemStatusId);

        // Order source
        _lookupResolverMock
            .Setup(x => x.GetIdAsync((ushort)LookupTypeEnum.OrderSource, It.Is<System.Enum>(e => e.ToString() == "DINE_IN"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(DineInSourceId);
        _lookupResolverMock
            .Setup(x => x.GetIdAsync((ushort)LookupTypeEnum.OrderSource, It.Is<System.Enum>(e => e.ToString() == "TAKEAWAY"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TakeawaySourceId);
    }

    private void SetupDefaultTax()
    {
        _taxRepoMock.Setup(x => x.GetDefaultTaxAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Tax
            {
                TaxId = 1,
                TaxName = "VAT",
                TaxRate = 10m,
                TaxType = "EXCLUSIVE",
                IsDefault = true,
                IsActive = true
            });
    }

    // ── Entity Helpers ──
    private static Order MakeOrder(
        long orderId = 1,
        uint statusId = PendingStatusId,
        long? tableId = 10,
        long? staffId = 5,
        long customerId = 1,
        decimal totalAmount = 100_000m,
        decimal subTotalAmount = 100_000m) => new()
        {
            OrderId = orderId,
            OrderStatusLvId = statusId,
            TableId = tableId,
            StaffId = staffId,
            CustomerId = customerId,
            TotalAmount = totalAmount,
            SubTotalAmount = subTotalAmount,
            CreatedAt = new DateTime(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc),
            OrderStatusLv = new LookupValue { ValueId = statusId, ValueCode = "PENDING" },
            Payments = new List<Payment>(),
            OrderItems = new List<OrderItem>()
        };

    private static RestaurantTable MakeTable(
        long tableId = 10,
        string tableCode = "TB-001",
        uint statusId = AvailableTableStatusId) => new()
        {
            TableId = tableId,
            TableCode = tableCode,
            Capacity = 4,
            TableStatusLvId = statusId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false,
            QrToken = "valid-qr-token"
        };

    private static Dish MakeDish(long dishId = 1, string name = "Pho Bo", decimal price = 50_000m) => new()
    {
        DishId = dishId,
        DishName = name,
        Price = price,
        CategoryId = 1,
        CreatedAt = DateTime.UtcNow
    };

    private static OrderItem MakeOrderItem(
        long orderItemId = 1,
        long orderId = 1,
        long dishId = 1,
        int qty = 2,
        decimal price = 50_000m,
        uint statusId = CreatedItemStatusId) => new()
        {
            OrderItemId = orderItemId,
            OrderId = orderId,
            DishId = dishId,
            Quantity = qty,
            Price = price,
            ItemStatusLvId = statusId,
            Dish = MakeDish(dishId),
            Order = new Order
            {
                OrderId = orderId,
                Table = MakeTable()
            }
        };

    // ══════════════════════════════════════════════════════════════════
    // GetOrderHistoryAsync
    // ══════════════════════════════════════════════════════════════════

    #region GetOrderHistoryAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetOrderHistoryAsync")]
    public async Task GetOrderHistoryAsync_WhenValidQuery_ReturnsPagedResult()
    {
        // Arrange
        var query = new OrderHistoryQueryDTO { PageIndex = 1, PageSize = 10 };
        var expected = new PagedResultDTO<OrderHistoryDTO>
        {
            PageData = new List<OrderHistoryDTO>
            {
                new() { OrderId = 1, TableCode = "TB-001", StaffName = "Staff A", TotalAmount = 100_000m, OrderStatus = "PENDING" },
                new() { OrderId = 2, TableCode = "TB-002", StaffName = "Staff B", TotalAmount = 200_000m, OrderStatus = "COMPLETED" }
            },
            PageIndex = 1,
            PageSize = 10,
            TotalCount = 2
        };
        _orderRepoMock.Setup(x => x.GetOrderHistoryAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var service = CreateService();

        // Act
        var result = await service.GetOrderHistoryAsync(query, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(expected);
        result.PageData.Should().HaveCount(2);
        result.PageData[0].OrderId.Should().Be(1);
        result.PageData[1].OrderId.Should().Be(2);
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetOrderHistoryAsync")]
    public async Task GetOrderHistoryAsync_WhenFilteredByStatus_ReturnsFilteredResult()
    {
        // Arrange
        var query = new OrderHistoryQueryDTO
        {
            PageIndex = 1,
            PageSize = 10,
            OrderStatusCode = OrderStatusCode.COMPLETED
        };
        var expected = new PagedResultDTO<OrderHistoryDTO>
        {
            PageData = new List<OrderHistoryDTO>
            {
                new() { OrderId = 3, OrderStatus = "COMPLETED", TotalAmount = 150_000m }
            },
            PageIndex = 1,
            PageSize = 10,
            TotalCount = 1
        };
        _orderRepoMock.Setup(x => x.GetOrderHistoryAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var service = CreateService();

        // Act
        var result = await service.GetOrderHistoryAsync(query, CancellationToken.None);

        // Assert
        result.PageData.Should().HaveCount(1);
        result.PageData[0].OrderStatus.Should().Be("COMPLETED");
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetOrderHistoryAsync")]
    public async Task GetOrderHistoryAsync_WhenNoOrders_ReturnsEmptyPage()
    {
        // Arrange
        var query = new OrderHistoryQueryDTO { PageIndex = 1, PageSize = 10 };
        var expected = new PagedResultDTO<OrderHistoryDTO>
        {
            PageData = new List<OrderHistoryDTO>(),
            PageIndex = 1,
            PageSize = 10,
            TotalCount = 0
        };
        _orderRepoMock.Setup(x => x.GetOrderHistoryAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var service = CreateService();

        // Act
        var result = await service.GetOrderHistoryAsync(query, CancellationToken.None);

        // Assert
        result.PageData.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.TotalPage.Should().Be(0);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetOrderHistoryAsync")]
    public async Task GetOrderHistoryAsync_WhenSearchByKeyword_DelegatesToRepo()
    {
        // Arrange
        var query = new OrderHistoryQueryDTO { PageIndex = 1, PageSize = 5, Search = "TB-001" };
        var expected = new PagedResultDTO<OrderHistoryDTO>
        {
            PageData = new List<OrderHistoryDTO>
            {
                new() { OrderId = 10, TableCode = "TB-001", TotalAmount = 80_000m }
            },
            PageIndex = 1,
            PageSize = 5,
            TotalCount = 1
        };
        _orderRepoMock.Setup(x => x.GetOrderHistoryAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var service = CreateService();

        // Act
        var result = await service.GetOrderHistoryAsync(query, CancellationToken.None);

        // Assert
        result.PageData.Should().HaveCount(1);
        result.PageData[0].TableCode.Should().Be("TB-001");
        _orderRepoMock.Verify(x => x.GetOrderHistoryAsync(query, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    // ══════════════════════════════════════════════════════════════════
    // GetOrderStatusCountAsync
    // ══════════════════════════════════════════════════════════════════

    #region GetOrderStatusCountAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetOrderStatusCountAsync")]
    public async Task GetOrderStatusCountAsync_WhenCalled_ReturnsCorrectCounts()
    {
        // Arrange
        SetupLookupResolver();
        var expected = new OrderStatusCountDTO
        {
            All = 25,
            Pending = 5,
            InProgress = 8,
            Completed = 10,
            Cancelled = 2
        };
        _orderRepoMock.Setup(x => x.GetOrderStatusCountAsync(
                PendingStatusId, InProgressStatusId, CompletedStatusId, CancelledStatusId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var service = CreateService();

        // Act
        var result = await service.GetOrderStatusCountAsync(CancellationToken.None);

        // Assert
        result.All.Should().Be(25);
        result.Pending.Should().Be(5);
        result.InProgress.Should().Be(8);
        result.Completed.Should().Be(10);
        result.Cancelled.Should().Be(2);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetOrderStatusCountAsync")]
    public async Task GetOrderStatusCountAsync_WhenAllZero_ReturnsZeroCounts()
    {
        // Arrange
        SetupLookupResolver();
        var expected = new OrderStatusCountDTO
        {
            All = 0,
            Pending = 0,
            InProgress = 0,
            Completed = 0,
            Cancelled = 0
        };
        _orderRepoMock.Setup(x => x.GetOrderStatusCountAsync(
                PendingStatusId, InProgressStatusId, CompletedStatusId, CancelledStatusId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var service = CreateService();

        // Act
        var result = await service.GetOrderStatusCountAsync(CancellationToken.None);

        // Assert
        result.All.Should().Be(0);
        result.Pending.Should().Be(0);
        result.InProgress.Should().Be(0);
        result.Completed.Should().Be(0);
        result.Cancelled.Should().Be(0);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetOrderStatusCountAsync")]
    public async Task GetOrderStatusCountAsync_ResolvesAllFourStatusIds()
    {
        // Arrange
        SetupLookupResolver();
        _orderRepoMock.Setup(x => x.GetOrderStatusCountAsync(
                PendingStatusId, InProgressStatusId, CompletedStatusId, CancelledStatusId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OrderStatusCountDTO());
        var service = CreateService();

        // Act
        await service.GetOrderStatusCountAsync(CancellationToken.None);

        // Assert
        _orderRepoMock.Verify(x => x.GetOrderStatusCountAsync(
            PendingStatusId, InProgressStatusId, CompletedStatusId, CancelledStatusId,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    // ══════════════════════════════════════════════════════════════════
    // GetKitchenOrdersAsync
    // ══════════════════════════════════════════════════════════════════

    #region GetKitchenOrdersAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetKitchenOrdersAsync")]
    public async Task GetKitchenOrdersAsync_WhenCalled_ReturnsKitchenOrders()
    {
        // Arrange
        SetupLookupResolver();
        var expected = new List<KitchenOrderDTO>
        {
            new()
            {
                OrderId = 1, TableCode = "TB-001", OrderStatus = "PENDING",
                CreatedAt = new DateTime(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc),
                Items = new List<KitchenOrderItemDTO>
                {
                    new() { OrderItemId = 10, DishName = "Pho Bo", Quantity = 2, ItemStatus = "CREATED" }
                }
            },
            new()
            {
                OrderId = 2, TableCode = "TB-002", OrderStatus = "IN_PROGRESS",
                CreatedAt = new DateTime(2025, 6, 1, 12, 5, 0, DateTimeKind.Utc),
                Items = new List<KitchenOrderItemDTO>
                {
                    new() { OrderItemId = 20, DishName = "Bun Cha", Quantity = 1, ItemStatus = "IN_PROGRESS" }
                }
            }
        };
        _orderRepoMock.Setup(x => x.GetKitchenOrdersAsync(
                PendingStatusId, InProgressStatusId, CompletedStatusId, CancelledStatusId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var service = CreateService();

        // Act
        var result = await service.GetKitchenOrdersAsync(CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result[0].OrderId.Should().Be(1);
        result[0].TableCode.Should().Be("TB-001");
        result[0].Items.Should().HaveCount(1);
        result[0].Items[0].DishName.Should().Be("Pho Bo");
        result[1].OrderId.Should().Be(2);
        result[1].OrderStatus.Should().Be("IN_PROGRESS");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetKitchenOrdersAsync")]
    public async Task GetKitchenOrdersAsync_WhenNoActiveOrders_ReturnsEmptyList()
    {
        // Arrange
        SetupLookupResolver();
        _orderRepoMock.Setup(x => x.GetKitchenOrdersAsync(
                PendingStatusId, InProgressStatusId, CompletedStatusId, CancelledStatusId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<KitchenOrderDTO>());
        var service = CreateService();

        // Act
        var result = await service.GetKitchenOrdersAsync(CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetKitchenOrdersAsync")]
    public async Task GetKitchenOrdersAsync_ResolvesAllFourStatusIds()
    {
        // Arrange
        SetupLookupResolver();
        _orderRepoMock.Setup(x => x.GetKitchenOrdersAsync(
                PendingStatusId, InProgressStatusId, CompletedStatusId, CancelledStatusId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<KitchenOrderDTO>());
        var service = CreateService();

        // Act
        await service.GetKitchenOrdersAsync(CancellationToken.None);

        // Assert
        _orderRepoMock.Verify(x => x.GetKitchenOrdersAsync(
            PendingStatusId, InProgressStatusId, CompletedStatusId, CancelledStatusId,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    // ══════════════════════════════════════════════════════════════════
    // UpdateOrderStatusAsync
    // ══════════════════════════════════════════════════════════════════

    #region UpdateOrderStatusAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "UpdateOrderStatusAsync")]
    public async Task UpdateOrderStatusAsync_PendingToInProgress_UpdatesStatus()
    {
        // Arrange
        SetupLookupResolver();
        var order = MakeOrder(orderId: 1, statusId: PendingStatusId, tableId: 10);
        _orderRepoMock.Setup(x => x.GetByIdForUpdateAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _tableRepoMock.Setup(x => x.GetByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeTable(10, "TB-001", OccupiedTableStatusId));
        _uowMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _realtimeMock.Setup(x => x.OrderUpdatedAsync(It.IsAny<OrderRealtimeDTO>())).Returns(Task.CompletedTask);
        _shiftLiveMock.Setup(x => x.PublishBoardChangedAsync(It.IsAny<ShiftLiveRealtimeEventDto>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var service = CreateService();

        // Act
        await service.UpdateOrderStatusAsync(1, OrderStatusCode.IN_PROGRESS, CancellationToken.None);

        // Assert
        order.OrderStatusLvId.Should().Be(InProgressStatusId);
        _uowMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "UpdateOrderStatusAsync")]
    public async Task UpdateOrderStatusAsync_PendingToCancelled_SetsTableAvailable()
    {
        // Arrange
        SetupLookupResolver();
        var order = MakeOrder(orderId: 2, statusId: PendingStatusId, tableId: 10);
        var table = MakeTable(10, "TB-001", OccupiedTableStatusId);
        _orderRepoMock.Setup(x => x.GetByIdForUpdateAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _tableRepoMock.Setup(x => x.GetByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(table);
        _uowMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _tableRepoMock.Setup(x => x.UpdateAsync(It.IsAny<RestaurantTable>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _notificationServiceMock.Setup(x => x.PublishAsync(It.IsAny<PublishNotificationRequest>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _realtimeMock.Setup(x => x.OrderUpdatedAsync(It.IsAny<OrderRealtimeDTO>())).Returns(Task.CompletedTask);
        _shiftLiveMock.Setup(x => x.PublishBoardChangedAsync(It.IsAny<ShiftLiveRealtimeEventDto>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var service = CreateService();

        // Act
        await service.UpdateOrderStatusAsync(2, OrderStatusCode.CANCELLED, CancellationToken.None);

        // Assert
        order.OrderStatusLvId.Should().Be(CancelledStatusId);
        table.TableStatusLvId.Should().Be(AvailableTableStatusId);
        _tableRepoMock.Verify(x => x.UpdateAsync(table, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "UpdateOrderStatusAsync")]
    public async Task UpdateOrderStatusAsync_InProgressToCompleted_SetsTableAvailable()
    {
        // Arrange
        SetupLookupResolver();
        var order = MakeOrder(orderId: 3, statusId: InProgressStatusId, tableId: 10);
        var table = MakeTable(10, "TB-001", OccupiedTableStatusId);
        _orderRepoMock.Setup(x => x.GetByIdForUpdateAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _tableRepoMock.Setup(x => x.GetByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(table);
        _uowMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _tableRepoMock.Setup(x => x.UpdateAsync(It.IsAny<RestaurantTable>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _realtimeMock.Setup(x => x.OrderUpdatedAsync(It.IsAny<OrderRealtimeDTO>())).Returns(Task.CompletedTask);
        _shiftLiveMock.Setup(x => x.PublishBoardChangedAsync(It.IsAny<ShiftLiveRealtimeEventDto>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var service = CreateService();

        // Act
        await service.UpdateOrderStatusAsync(3, OrderStatusCode.COMPLETED, CancellationToken.None);

        // Assert
        order.OrderStatusLvId.Should().Be(CompletedStatusId);
        table.TableStatusLvId.Should().Be(AvailableTableStatusId);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "UpdateOrderStatusAsync")]
    public async Task UpdateOrderStatusAsync_CancelledToPending_SetsTableOccupied()
    {
        // Arrange
        SetupLookupResolver();
        var order = MakeOrder(orderId: 4, statusId: CancelledStatusId, tableId: 10);
        var table = MakeTable(10, "TB-001", AvailableTableStatusId);
        _orderRepoMock.Setup(x => x.GetByIdForUpdateAsync(4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _tableRepoMock.Setup(x => x.GetByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(table);
        _uowMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _tableRepoMock.Setup(x => x.UpdateAsync(It.IsAny<RestaurantTable>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _realtimeMock.Setup(x => x.OrderUpdatedAsync(It.IsAny<OrderRealtimeDTO>())).Returns(Task.CompletedTask);
        _shiftLiveMock.Setup(x => x.PublishBoardChangedAsync(It.IsAny<ShiftLiveRealtimeEventDto>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var service = CreateService();

        // Act
        await service.UpdateOrderStatusAsync(4, OrderStatusCode.PENDING, CancellationToken.None);

        // Assert
        order.OrderStatusLvId.Should().Be(PendingStatusId);
        table.TableStatusLvId.Should().Be(OccupiedTableStatusId);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "UpdateOrderStatusAsync")]
    public async Task UpdateOrderStatusAsync_CompletedToInProgress_AllowedTransition()
    {
        // Arrange
        SetupLookupResolver();
        var order = MakeOrder(orderId: 5, statusId: CompletedStatusId, tableId: null);
        _orderRepoMock.Setup(x => x.GetByIdForUpdateAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _uowMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _realtimeMock.Setup(x => x.OrderUpdatedAsync(It.IsAny<OrderRealtimeDTO>())).Returns(Task.CompletedTask);
        _shiftLiveMock.Setup(x => x.PublishBoardChangedAsync(It.IsAny<ShiftLiveRealtimeEventDto>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var service = CreateService();

        // Act
        await service.UpdateOrderStatusAsync(5, OrderStatusCode.IN_PROGRESS, CancellationToken.None);

        // Assert
        order.OrderStatusLvId.Should().Be(InProgressStatusId);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "UpdateOrderStatusAsync")]
    public async Task UpdateOrderStatusAsync_WhenOrderNotFound_ThrowsNotFoundException()
    {
        // Arrange
        SetupLookupResolver();
        _orderRepoMock.Setup(x => x.GetByIdForUpdateAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);
        _uowMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(x => x.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var service = CreateService();

        // Act
        Func<Task> act = () => service.UpdateOrderStatusAsync(999, OrderStatusCode.IN_PROGRESS, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Order not found.");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "UpdateOrderStatusAsync")]
    public async Task UpdateOrderStatusAsync_WhenSameStatus_ThrowsInvalidOperationException()
    {
        // Arrange
        SetupLookupResolver();
        var order = MakeOrder(orderId: 6, statusId: PendingStatusId);
        _orderRepoMock.Setup(x => x.GetByIdForUpdateAsync(6, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _uowMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(x => x.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var service = CreateService();

        // Act
        Func<Task> act = () => service.UpdateOrderStatusAsync(6, OrderStatusCode.PENDING, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Order is already in this status.");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "UpdateOrderStatusAsync")]
    public async Task UpdateOrderStatusAsync_InvalidTransition_PendingToCompleted_ThrowsInvalidOperationException()
    {
        // Arrange
        SetupLookupResolver();
        var order = MakeOrder(orderId: 7, statusId: PendingStatusId);
        _orderRepoMock.Setup(x => x.GetByIdForUpdateAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _uowMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(x => x.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var service = CreateService();

        // Act
        Func<Task> act = () => service.UpdateOrderStatusAsync(7, OrderStatusCode.COMPLETED, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Invalid status transition: PENDING -> COMPLETED.");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "UpdateOrderStatusAsync")]
    public async Task UpdateOrderStatusAsync_CancelPaidOrder_ThrowsInvalidOperationException()
    {
        // Arrange
        SetupLookupResolver();
        var order = MakeOrder(orderId: 8, statusId: InProgressStatusId);
        order.Payments = new List<Payment> { new() { PaymentId = 1, OrderId = 8, ReceivedAmount = 100_000m } };
        _orderRepoMock.Setup(x => x.GetByIdForUpdateAsync(8, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _uowMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(x => x.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var service = CreateService();

        // Act
        Func<Task> act = () => service.UpdateOrderStatusAsync(8, OrderStatusCode.CANCELLED, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Cannot cancel a paid order.");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "UpdateOrderStatusAsync")]
    public async Task UpdateOrderStatusAsync_CompletedToPending_InvalidTransition_Throws()
    {
        // Arrange
        SetupLookupResolver();
        var order = MakeOrder(orderId: 9, statusId: CompletedStatusId);
        _orderRepoMock.Setup(x => x.GetByIdForUpdateAsync(9, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _uowMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(x => x.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var service = CreateService();

        // Act
        Func<Task> act = () => service.UpdateOrderStatusAsync(9, OrderStatusCode.PENDING, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Invalid status transition: COMPLETED -> PENDING.");
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "UpdateOrderStatusAsync")]
    public async Task UpdateOrderStatusAsync_WhenCancelled_SendsNotification()
    {
        // Arrange
        SetupLookupResolver();
        var order = MakeOrder(orderId: 11, statusId: PendingStatusId, tableId: null);
        _orderRepoMock.Setup(x => x.GetByIdForUpdateAsync(11, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _uowMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _notificationServiceMock.Setup(x => x.PublishAsync(It.IsAny<PublishNotificationRequest>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _realtimeMock.Setup(x => x.OrderUpdatedAsync(It.IsAny<OrderRealtimeDTO>())).Returns(Task.CompletedTask);
        _shiftLiveMock.Setup(x => x.PublishBoardChangedAsync(It.IsAny<ShiftLiveRealtimeEventDto>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var service = CreateService();

        // Act
        await service.UpdateOrderStatusAsync(11, OrderStatusCode.CANCELLED, CancellationToken.None);

        // Assert
        _notificationServiceMock.Verify(x => x.PublishAsync(
            It.Is<PublishNotificationRequest>(n =>
                n.Type == nameof(NotificationType.ORDER_CANCELLED) &&
                n.EntityId == "11"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "UpdateOrderStatusAsync")]
    public async Task UpdateOrderStatusAsync_WhenNoTable_SkipsTableUpdate()
    {
        // Arrange
        SetupLookupResolver();
        var order = MakeOrder(orderId: 12, statusId: InProgressStatusId, tableId: null);
        _orderRepoMock.Setup(x => x.GetByIdForUpdateAsync(12, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _uowMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _realtimeMock.Setup(x => x.OrderUpdatedAsync(It.IsAny<OrderRealtimeDTO>())).Returns(Task.CompletedTask);
        _shiftLiveMock.Setup(x => x.PublishBoardChangedAsync(It.IsAny<ShiftLiveRealtimeEventDto>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var service = CreateService();

        // Act
        await service.UpdateOrderStatusAsync(12, OrderStatusCode.COMPLETED, CancellationToken.None);

        // Assert
        order.OrderStatusLvId.Should().Be(CompletedStatusId);
        _tableRepoMock.Verify(x => x.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "UpdateOrderStatusAsync")]
    public async Task UpdateOrderStatusAsync_WhenOrderIdNegative_ThrowsNotFoundException()
    {
        // Arrange
        SetupLookupResolver();
        _orderRepoMock.Setup(x => x.GetByIdForUpdateAsync(-1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);
        _uowMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(x => x.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var service = CreateService();

        // Act
        Func<Task> act = () => service.UpdateOrderStatusAsync(-1, OrderStatusCode.IN_PROGRESS, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Order not found.");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "UpdateOrderStatusAsync")]
    public async Task UpdateOrderStatusAsync_WhenOrderIdMaxValue_ThrowsNotFoundException()
    {
        // Arrange
        SetupLookupResolver();
        _orderRepoMock.Setup(x => x.GetByIdForUpdateAsync(long.MaxValue, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);
        _uowMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(x => x.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var service = CreateService();

        // Act
        Func<Task> act = () => service.UpdateOrderStatusAsync(long.MaxValue, OrderStatusCode.CANCELLED, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Order not found.");
    }

    #endregion

    // ══════════════════════════════════════════════════════════════════
    // CancelOrderItemAsync
    // ══════════════════════════════════════════════════════════════════

    #region CancelOrderItemAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "CancelOrderItemAsync")]
    public async Task CancelOrderItemAsync_WhenValidItem_CancelsAndNotifies()
    {
        // Arrange
        SetupLookupResolver();
        var orderItem = MakeOrderItem(orderItemId: 50, orderId: 1, dishId: 1);
        _orderRepoMock.Setup(x => x.GetOrderItemAsync(50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(orderItem);
        _orderRepoMock.Setup(x => x.UpdateOrderItemStatusAsync(
                50, CancelledItemStatusId, null,
                InProgressItemStatusId, ReadyItemStatusId, ServedItemStatusId, RejectedItemStatusId, CancelledItemStatusId,
                PendingStatusId, InProgressStatusId, CompletedStatusId, CancelledStatusId, AvailableTableStatusId,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _notificationServiceMock.Setup(x => x.PublishAsync(It.IsAny<PublishNotificationRequest>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _shiftLiveMock.Setup(x => x.PublishBoardChangedAsync(It.IsAny<ShiftLiveRealtimeEventDto>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var service = CreateService();

        // Act
        await service.CancelOrderItemAsync(50, CancellationToken.None);

        // Assert
        _orderRepoMock.Verify(x => x.UpdateOrderItemStatusAsync(
            50, CancelledItemStatusId, null,
            It.IsAny<uint>(), It.IsAny<uint>(), It.IsAny<uint>(), It.IsAny<uint>(), It.IsAny<uint>(),
            It.IsAny<uint>(), It.IsAny<uint>(), It.IsAny<uint>(), It.IsAny<uint>(), It.IsAny<uint>(),
            It.IsAny<CancellationToken>()), Times.Once);
        _notificationServiceMock.Verify(x => x.PublishAsync(
            It.Is<PublishNotificationRequest>(n => n.Type == nameof(NotificationType.ORDER_ITEM_CANCELLED)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "CancelOrderItemAsync")]
    public async Task CancelOrderItemAsync_NotificationContainsDishAndTableInfo()
    {
        // Arrange
        SetupLookupResolver();
        var dish = MakeDish(1, "Pho Bo", 50_000m);
        var table = MakeTable(10, "TB-001");
        var orderItem = new OrderItem
        {
            OrderItemId = 51,
            OrderId = 1,
            DishId = 1,
            Quantity = 2,
            Price = 50_000m,
            ItemStatusLvId = CreatedItemStatusId,
            Dish = dish,
            Order = new Order { OrderId = 1, Table = table }
        };
        _orderRepoMock.Setup(x => x.GetOrderItemAsync(51, It.IsAny<CancellationToken>()))
            .ReturnsAsync(orderItem);
        _orderRepoMock.Setup(x => x.UpdateOrderItemStatusAsync(
                51, CancelledItemStatusId, null,
                It.IsAny<uint>(), It.IsAny<uint>(), It.IsAny<uint>(), It.IsAny<uint>(), It.IsAny<uint>(),
                It.IsAny<uint>(), It.IsAny<uint>(), It.IsAny<uint>(), It.IsAny<uint>(), It.IsAny<uint>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _notificationServiceMock.Setup(x => x.PublishAsync(It.IsAny<PublishNotificationRequest>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _shiftLiveMock.Setup(x => x.PublishBoardChangedAsync(It.IsAny<ShiftLiveRealtimeEventDto>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var service = CreateService();

        // Act
        await service.CancelOrderItemAsync(51, CancellationToken.None);

        // Assert
        _notificationServiceMock.Verify(x => x.PublishAsync(
            It.Is<PublishNotificationRequest>(n =>
                n.Metadata != null &&
                n.Metadata.ContainsKey("dishName") &&
                (string)n.Metadata["dishName"] == "Pho Bo" &&
                n.Metadata.ContainsKey("tableCode") &&
                (string)n.Metadata["tableCode"] == "TB-001"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "CancelOrderItemAsync")]
    public async Task CancelOrderItemAsync_WhenItemHasNoDish_HandlesGracefully()
    {
        // Arrange
        SetupLookupResolver();
        var orderItem = new OrderItem
        {
            OrderItemId = 52,
            OrderId = 1,
            DishId = 1,
            Quantity = 1,
            Price = 30_000m,
            ItemStatusLvId = CreatedItemStatusId,
            Dish = null!,
            Order = new Order { OrderId = 1, Table = null }
        };
        _orderRepoMock.Setup(x => x.GetOrderItemAsync(52, It.IsAny<CancellationToken>()))
            .ReturnsAsync(orderItem);
        _orderRepoMock.Setup(x => x.UpdateOrderItemStatusAsync(
                52, CancelledItemStatusId, null,
                It.IsAny<uint>(), It.IsAny<uint>(), It.IsAny<uint>(), It.IsAny<uint>(), It.IsAny<uint>(),
                It.IsAny<uint>(), It.IsAny<uint>(), It.IsAny<uint>(), It.IsAny<uint>(), It.IsAny<uint>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _notificationServiceMock.Setup(x => x.PublishAsync(It.IsAny<PublishNotificationRequest>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _shiftLiveMock.Setup(x => x.PublishBoardChangedAsync(It.IsAny<ShiftLiveRealtimeEventDto>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var service = CreateService();

        // Act
        await service.CancelOrderItemAsync(52, CancellationToken.None);

        // Assert
        _notificationServiceMock.Verify(x => x.PublishAsync(
            It.Is<PublishNotificationRequest>(n =>
                n.Metadata != null &&
                (string)n.Metadata["dishName"] == "" &&
                (string)n.Metadata["tableCode"] == ""),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    // ══════════════════════════════════════════════════════════════════
    // GetOrderByIdAsync
    // ══════════════════════════════════════════════════════════════════

    #region GetOrderByIdAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetOrderByIdAsync")]
    public async Task GetOrderByIdAsync_WhenOrderExists_ReturnsDetail()
    {
        // Arrange
        var expected = new OrderDetailDTO
        {
            OrderId = 1,
            TableCode = "TB-001",
            StaffName = "Staff A",
            TotalAmount = 150_000m,
            OrderStatus = "PENDING",
            Source = "DINE_IN"
        };
        _orderRepoMock.Setup(x => x.GetOrderByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var service = CreateService();

        // Act
        var result = await service.GetOrderByIdAsync(1, CancellationToken.None);

        // Assert
        result.OrderId.Should().Be(1);
        result.TableCode.Should().Be("TB-001");
        result.TotalAmount.Should().Be(150_000m);
        result.OrderStatus.Should().Be("PENDING");
        result.Source.Should().Be("DINE_IN");
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetOrderByIdAsync")]
    public async Task GetOrderByIdAsync_DelegatesToRepository()
    {
        // Arrange
        _orderRepoMock.Setup(x => x.GetOrderByIdAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OrderDetailDTO { OrderId = 42 });
        var service = CreateService();

        // Act
        var result = await service.GetOrderByIdAsync(42, CancellationToken.None);

        // Assert
        result.OrderId.Should().Be(42);
        _orderRepoMock.Verify(x => x.GetOrderByIdAsync(42, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetOrderByIdAsync")]
    public async Task GetOrderByIdAsync_WhenRepoReturnsNull_ReturnsNull()
    {
        // Arrange
        _orderRepoMock.Setup(x => x.GetOrderByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OrderDetailDTO)null!);
        var service = CreateService();

        // Act
        var result = await service.GetOrderByIdAsync(999, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetOrderByIdAsync")]
    public async Task GetOrderByIdAsync_WhenOrderIdIsMaxValue_ReturnsNull()
    {
        // Arrange
        _orderRepoMock.Setup(x => x.GetOrderByIdAsync(long.MaxValue, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OrderDetailDTO)null!);
        var service = CreateService();

        // Act
        var result = await service.GetOrderByIdAsync(long.MaxValue, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "GetOrderByIdAsync")]
    public async Task GetOrderByIdAsync_WhenOrderIdIsNegative()
    {
        // Arrange
        _orderRepoMock.Setup(x => x.GetOrderByIdAsync(-1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OrderDetailDTO)null!);
        var service = CreateService();

        // Act
        var result = await service.GetOrderByIdAsync(-1, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    // ══════════════════════════════════════════════════════════════════
    // CreateOrderAsync (Staff)
    // ══════════════════════════════════════════════════════════════════

    #region CreateOrderAsync_Staff

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "CreateOrderAsync_Staff")]
    public async Task CreateOrderAsync_Staff_DineIn_CreatesOrderWithTable()
    {
        // Arrange
        SetupLookupResolver();
        SetupDefaultTax();
        var request = new CreateOrderRequest
        {
            TableId = 10,
            Source = OrderSourceCode.DINE_IN,
            Customer = new OrderCustomerDto { Phone = "0901234567", FullName = "Nguyen Van A" },
            Items = new List<CreateOrderItemDto>
            {
                new() { DishId = 1, Quantity = 2 },
                new() { DishId = 2, Quantity = 1 }
            }
        };
        var table = MakeTable(10, "TB-001", AvailableTableStatusId);
        _tableRepoMock.Setup(x => x.GetByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(table);
        _tableRepoMock.Setup(x => x.UpdateAsync(It.IsAny<RestaurantTable>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _customerServiceMock.Setup(x => x.ResolveCustomerAsync(request.Customer, It.IsAny<CancellationToken>()))
            .ReturnsAsync(100L);
        _dishRepoMock.Setup(x => x.GetByIdsAsync(It.IsAny<List<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Dish>
            {
                MakeDish(1, "Pho Bo", 50_000m),
                MakeDish(2, "Bun Cha", 45_000m)
            });
        _orderRepoMock.Setup(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Callback<Order, CancellationToken>((o, _) => o.OrderId = 1001);
        _uowMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _realtimeMock.Setup(x => x.OrderCreatedAsync(It.IsAny<OrderRealtimeDTO>())).Returns(Task.CompletedTask);
        _shiftLiveMock.Setup(x => x.PublishBoardChangedAsync(It.IsAny<ShiftLiveRealtimeEventDto>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _notificationServiceMock.Setup(x => x.PublishAsync(It.IsAny<PublishNotificationRequest>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var service = CreateService();

        // Act
        var result = await service.CreateOrderAsync(5L, request, CancellationToken.None);

        // Assert
        result.Should().Be(1001);
        table.TableStatusLvId.Should().Be(OccupiedTableStatusId);
        _orderRepoMock.Verify(x => x.AddAsync(It.Is<Order>(o =>
            o.StaffId == 5 &&
            o.CustomerId == 100 &&
            o.TableId == 10 &&
            o.TotalAmount == 145_000m &&
            o.OrderStatusLvId == PendingStatusId &&
            o.SourceLvId == DineInSourceId &&
            o.OrderItems.Count == 2
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "CreateOrderAsync_Staff")]
    public async Task CreateOrderAsync_Staff_Takeaway_CreatesOrderWithoutTable()
    {
        // Arrange
        SetupLookupResolver();
        SetupDefaultTax();
        var request = new CreateOrderRequest
        {
            TableId = null,
            Source = OrderSourceCode.TAKEAWAY,
            Customer = null,
            Items = new List<CreateOrderItemDto>
            {
                new() { DishId = 1, Quantity = 3 }
            }
        };
        _customerServiceMock.Setup(x => x.ResolveCustomerAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(999L);
        _dishRepoMock.Setup(x => x.GetByIdsAsync(It.IsAny<List<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Dish> { MakeDish(1, "Pho Bo", 50_000m) });
        _orderRepoMock.Setup(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Callback<Order, CancellationToken>((o, _) => o.OrderId = 1002);
        _uowMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _realtimeMock.Setup(x => x.OrderCreatedAsync(It.IsAny<OrderRealtimeDTO>())).Returns(Task.CompletedTask);
        _shiftLiveMock.Setup(x => x.PublishBoardChangedAsync(It.IsAny<ShiftLiveRealtimeEventDto>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _notificationServiceMock.Setup(x => x.PublishAsync(It.IsAny<PublishNotificationRequest>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var service = CreateService();

        // Act
        var result = await service.CreateOrderAsync(5L, request, CancellationToken.None);

        // Assert
        result.Should().Be(1002);
        _orderRepoMock.Verify(x => x.AddAsync(It.Is<Order>(o =>
            o.TableId == null &&
            o.SourceLvId == TakeawaySourceId &&
            o.TotalAmount == 150_000m
        ), It.IsAny<CancellationToken>()), Times.Once);
        _tableRepoMock.Verify(x => x.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CreateOrderAsync_Staff")]
    public async Task CreateOrderAsync_Staff_EmptyItems_ThrowsInvalidOperationException()
    {
        // Arrange
        var request = new CreateOrderRequest
        {
            Source = OrderSourceCode.DINE_IN,
            TableId = 10,
            Items = new List<CreateOrderItemDto>()
        };
        _uowMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(x => x.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var service = CreateService();

        // Act
        Func<Task> act = () => service.CreateOrderAsync(5L, request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Order must contain at least one item.");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CreateOrderAsync_Staff")]
    public async Task CreateOrderAsync_Staff_DineInWithoutTable_ThrowsInvalidOperationException()
    {
        // Arrange
        var request = new CreateOrderRequest
        {
            Source = OrderSourceCode.DINE_IN,
            TableId = null,
            Items = new List<CreateOrderItemDto> { new() { DishId = 1, Quantity = 1 } }
        };
        _uowMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(x => x.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var service = CreateService();

        // Act
        Func<Task> act = () => service.CreateOrderAsync(5L, request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("DINE_IN order requires table.");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CreateOrderAsync_Staff")]
    public async Task CreateOrderAsync_Staff_TakeawayWithTable_ThrowsInvalidOperationException()
    {
        // Arrange
        var request = new CreateOrderRequest
        {
            Source = OrderSourceCode.TAKEAWAY,
            TableId = 10,
            Items = new List<CreateOrderItemDto> { new() { DishId = 1, Quantity = 1 } }
        };
        _uowMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(x => x.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var service = CreateService();

        // Act
        Func<Task> act = () => service.CreateOrderAsync(5L, request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("TAKEAWAY cannot have table.");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CreateOrderAsync_Staff")]
    public async Task CreateOrderAsync_Staff_TableNotFound_ThrowsNotFoundException()
    {
        // Arrange
        SetupLookupResolver();
        var request = new CreateOrderRequest
        {
            Source = OrderSourceCode.DINE_IN,
            TableId = 999,
            Items = new List<CreateOrderItemDto> { new() { DishId = 1, Quantity = 1 } }
        };
        _tableRepoMock.Setup(x => x.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RestaurantTable?)null);
        _uowMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(x => x.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var service = CreateService();

        // Act
        Func<Task> act = () => service.CreateOrderAsync(5L, request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Table not found.");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CreateOrderAsync_Staff")]
    public async Task CreateOrderAsync_Staff_TableOccupied_ThrowsInvalidOperationException()
    {
        // Arrange
        SetupLookupResolver();
        var request = new CreateOrderRequest
        {
            Source = OrderSourceCode.DINE_IN,
            TableId = 10,
            Items = new List<CreateOrderItemDto> { new() { DishId = 1, Quantity = 1 } }
        };
        _tableRepoMock.Setup(x => x.GetByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeTable(10, "TB-001", OccupiedTableStatusId));
        _uowMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(x => x.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var service = CreateService();

        // Act
        Func<Task> act = () => service.CreateOrderAsync(5L, request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Table is not available.");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CreateOrderAsync_Staff")]
    public async Task CreateOrderAsync_Staff_DishNotFound_ThrowsNotFoundException()
    {
        // Arrange
        SetupLookupResolver();
        var request = new CreateOrderRequest
        {
            Source = OrderSourceCode.DINE_IN,
            TableId = 10,
            Customer = null,
            Items = new List<CreateOrderItemDto>
            {
                new() { DishId = 1, Quantity = 1 },
                new() { DishId = 999, Quantity = 1 }
            }
        };
        _tableRepoMock.Setup(x => x.GetByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeTable(10, "TB-001", AvailableTableStatusId));
        _tableRepoMock.Setup(x => x.UpdateAsync(It.IsAny<RestaurantTable>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _customerServiceMock.Setup(x => x.ResolveCustomerAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(999L);
        _dishRepoMock.Setup(x => x.GetByIdsAsync(It.IsAny<List<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Dish> { MakeDish(1, "Pho Bo", 50_000m) });
        _uowMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(x => x.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var service = CreateService();

        // Act
        Func<Task> act = () => service.CreateOrderAsync(5L, request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("One or more dishes not found.");
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "CreateOrderAsync_Staff")]
    public async Task CreateOrderAsync_Staff_CalculatesTotalCorrectly()
    {
        // Arrange
        SetupLookupResolver();
        SetupDefaultTax();
        var request = new CreateOrderRequest
        {
            Source = OrderSourceCode.DINE_IN,
            TableId = 10,
            Items = new List<CreateOrderItemDto>
            {
                new() { DishId = 1, Quantity = 2 },
                new() { DishId = 2, Quantity = 3 }
            }
        };
        _tableRepoMock.Setup(x => x.GetByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeTable(10, "TB-001", AvailableTableStatusId));
        _tableRepoMock.Setup(x => x.UpdateAsync(It.IsAny<RestaurantTable>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _customerServiceMock.Setup(x => x.ResolveCustomerAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(999L);
        _dishRepoMock.Setup(x => x.GetByIdsAsync(It.IsAny<List<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Dish>
            {
                MakeDish(1, "Pho Bo", 50_000m),
                MakeDish(2, "Bun Cha", 30_000m)
            });
        Order? capturedOrder = null;
        _orderRepoMock.Setup(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Callback<Order, CancellationToken>((o, _) => { capturedOrder = o; o.OrderId = 1003; });
        _uowMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _realtimeMock.Setup(x => x.OrderCreatedAsync(It.IsAny<OrderRealtimeDTO>())).Returns(Task.CompletedTask);
        _shiftLiveMock.Setup(x => x.PublishBoardChangedAsync(It.IsAny<ShiftLiveRealtimeEventDto>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _notificationServiceMock.Setup(x => x.PublishAsync(It.IsAny<PublishNotificationRequest>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var service = CreateService();

        // Act
        await service.CreateOrderAsync(5L, request, CancellationToken.None);

        // Assert — 2×50000 + 3×30000 = 190000
        capturedOrder.Should().NotBeNull();
        capturedOrder!.TotalAmount.Should().Be(190_000m);
        capturedOrder.SubTotalAmount.Should().Be(190_000m);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "CreateOrderAsync_Staff")]
    public async Task CreateOrderAsync_Staff_TableLocked_ThrowsInvalidOperationException()
    {
        // Arrange
        SetupLookupResolver();
        var request = new CreateOrderRequest
        {
            Source = OrderSourceCode.DINE_IN,
            TableId = 10,
            Items = new List<CreateOrderItemDto> { new() { DishId = 1, Quantity = 1 } }
        };
        _tableRepoMock.Setup(x => x.GetByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeTable(10, "TB-001", LockedTableStatusId));
        _uowMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(x => x.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var service = CreateService();

        // Act
        Func<Task> act = () => service.CreateOrderAsync(5L, request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Table is not available.");
    }

    #endregion

    // ══════════════════════════════════════════════════════════════════
    // AddItemsAsync (Staff)
    // ══════════════════════════════════════════════════════════════════

    #region AddItemsAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "AddItemsAsync")]
    public async Task AddItemsAsync_WhenValidOrder_AddsItemsAndUpdatesTotal()
    {
        // Arrange
        SetupLookupResolver();
        SetupDefaultTax();
        var order = MakeOrder(orderId: 20, statusId: PendingStatusId, totalAmount: 100_000m, subTotalAmount: 100_000m);
        order.OrderStatusLv = new LookupValue { ValueId = PendingStatusId, ValueCode = "PENDING" };
        var request = new AddOrderItemsRequest
        {
            Items = new List<CreateOrderItemDto>
            {
                new() { DishId = 3, Quantity = 2 }
            }
        };
        _orderRepoMock.Setup(x => x.GetByIdForUpdateAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _dishRepoMock.Setup(x => x.GetByIdsAsync(It.IsAny<List<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Dish> { MakeDish(3, "Com Tam", 40_000m) });
        _uowMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _realtimeMock.Setup(x => x.OrderUpdatedAsync(It.IsAny<OrderRealtimeDTO>())).Returns(Task.CompletedTask);
        _shiftLiveMock.Setup(x => x.PublishBoardChangedAsync(It.IsAny<ShiftLiveRealtimeEventDto>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var service = CreateService();

        // Act
        await service.AddItemsAsync(20, request, CancellationToken.None);

        // Assert — original 100000 + 2×40000 = 180000
        order.TotalAmount.Should().Be(180_000m);
        order.SubTotalAmount.Should().Be(180_000m);
        order.OrderItems.Should().HaveCount(1);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "AddItemsAsync")]
    public async Task AddItemsAsync_WhenOrderNotFound_ThrowsNotFoundException()
    {
        // Arrange
        SetupLookupResolver();
        _orderRepoMock.Setup(x => x.GetByIdForUpdateAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);
        _uowMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(x => x.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var service = CreateService();

        // Act
        Func<Task> act = () => service.AddItemsAsync(999, new AddOrderItemsRequest { Items = new() }, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Order not found.");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "AddItemsAsync")]
    public async Task AddItemsAsync_WhenOrderCancelled_ThrowsInvalidOperationException()
    {
        // Arrange
        SetupLookupResolver();
        var order = MakeOrder(orderId: 21, statusId: CancelledStatusId);
        _orderRepoMock.Setup(x => x.GetByIdForUpdateAsync(21, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _uowMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(x => x.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var service = CreateService();

        // Act
        Func<Task> act = () => service.AddItemsAsync(21, new AddOrderItemsRequest { Items = new() }, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Cannot add items to canceled order.");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "AddItemsAsync")]
    public async Task AddItemsAsync_WhenOrderPaid_ThrowsInvalidOperationException()
    {
        // Arrange
        SetupLookupResolver();
        var order = MakeOrder(orderId: 22, statusId: PendingStatusId);
        order.Payments = new List<Payment> { new() { PaymentId = 1, OrderId = 22, ReceivedAmount = 100_000m } };
        _orderRepoMock.Setup(x => x.GetByIdForUpdateAsync(22, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _uowMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(x => x.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var service = CreateService();

        // Act
        Func<Task> act = () => service.AddItemsAsync(22, new AddOrderItemsRequest { Items = new() }, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Cannot add items to paid order.");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "AddItemsAsync")]
    public async Task AddItemsAsync_WhenDishNotFound_ThrowsNotFoundException()
    {
        // Arrange
        SetupLookupResolver();
        var order = MakeOrder(orderId: 23, statusId: InProgressStatusId);
        var request = new AddOrderItemsRequest
        {
            Items = new List<CreateOrderItemDto>
            {
                new() { DishId = 1, Quantity = 1 },
                new() { DishId = 888, Quantity = 1 }
            }
        };
        _orderRepoMock.Setup(x => x.GetByIdForUpdateAsync(23, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _dishRepoMock.Setup(x => x.GetByIdsAsync(It.IsAny<List<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Dish> { MakeDish(1) });
        _uowMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(x => x.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var service = CreateService();

        // Act
        Func<Task> act = () => service.AddItemsAsync(23, request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("One or more dishes not found.");
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "AddItemsAsync")]
    public async Task AddItemsAsync_WhenCompletedOrder_TransitionsToInProgress()
    {
        // Arrange
        SetupLookupResolver();
        SetupDefaultTax();
        var order = MakeOrder(orderId: 24, statusId: CompletedStatusId, totalAmount: 50_000m, subTotalAmount: 50_000m);
        order.OrderStatusLv = new LookupValue { ValueId = CompletedStatusId, ValueCode = "COMPLETED" };
        var request = new AddOrderItemsRequest
        {
            Items = new List<CreateOrderItemDto> { new() { DishId = 1, Quantity = 1 } }
        };
        _orderRepoMock.Setup(x => x.GetByIdForUpdateAsync(24, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _dishRepoMock.Setup(x => x.GetByIdsAsync(It.IsAny<List<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Dish> { MakeDish(1, "Pho Bo", 50_000m) });
        _uowMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _realtimeMock.Setup(x => x.OrderUpdatedAsync(It.IsAny<OrderRealtimeDTO>())).Returns(Task.CompletedTask);
        _shiftLiveMock.Setup(x => x.PublishBoardChangedAsync(It.IsAny<ShiftLiveRealtimeEventDto>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var service = CreateService();

        // Act
        await service.AddItemsAsync(24, request, CancellationToken.None);

        // Assert
        order.OrderStatusLvId.Should().Be(InProgressStatusId);
        order.TotalAmount.Should().Be(100_000m);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "AddItemsAsync")]
    public async Task AddItemsAsync_WhenCustomerProvided_ResolvesCustomer()
    {
        // Arrange
        SetupLookupResolver();
        SetupDefaultTax();
        var order = MakeOrder(orderId: 25, statusId: PendingStatusId);
        order.OrderStatusLv = new LookupValue { ValueId = PendingStatusId, ValueCode = "PENDING" };
        order.CustomerId = 100;
        var request = new AddOrderItemsRequest
        {
            Customer = new OrderCustomerDto { Phone = "0909999999", FullName = "New Customer" },
            Items = new List<CreateOrderItemDto>()
        };
        _orderRepoMock.Setup(x => x.GetByIdForUpdateAsync(25, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _customerServiceMock.Setup(x => x.ResolveCustomerAsync(request.Customer, It.IsAny<CancellationToken>()))
            .ReturnsAsync(200L);
        _uowMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _realtimeMock.Setup(x => x.OrderUpdatedAsync(It.IsAny<OrderRealtimeDTO>())).Returns(Task.CompletedTask);
        _shiftLiveMock.Setup(x => x.PublishBoardChangedAsync(It.IsAny<ShiftLiveRealtimeEventDto>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var service = CreateService();

        // Act
        await service.AddItemsAsync(25, request, CancellationToken.None);

        // Assert
        order.CustomerId.Should().Be(200L);
    }

    #endregion

    // ══════════════════════════════════════════════════════════════════
    // CreateOrderAsync (Customer)
    // ══════════════════════════════════════════════════════════════════

    #region CreateOrderAsync_Customer

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "CreateOrderAsync_Customer")]
    public async Task CreateOrderAsync_Customer_WhenTableAvailable_CreatesNewOrder()
    {
        // Arrange
        SetupLookupResolver();
        SetupDefaultTax();
        var request = new CreateOrderRequestDTO
        {
            TableCode = "TB-001",
            QrToken = "valid-qr-token",
            Items = new List<CreateOrderItemDTO>
            {
                new() { DishId = 1, Quantity = 2, Price = 50_000m },
                new() { DishId = 2, Quantity = 1, Price = 45_000m }
            }
        };
        var table = MakeTable(10, "TB-001", AvailableTableStatusId);
        _tableRepoMock.Setup(x => x.GetByCodeAsync("TB-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(table);
        _tableRepoMock.Setup(x => x.UpdateAsync(It.IsAny<RestaurantTable>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _customerServiceMock.Setup(x => x.GetGuestCustomerIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(68L);
        _orderRepoMock.Setup(x => x.CreateOrderAsync(It.IsAny<Order>(), It.IsAny<List<OrderItem>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(2001L);
        _uowMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _realtimeMock.Setup(x => x.OrderCreatedAsync(It.IsAny<OrderRealtimeDTO>())).Returns(Task.CompletedTask);
        _shiftLiveMock.Setup(x => x.PublishBoardChangedAsync(It.IsAny<ShiftLiveRealtimeEventDto>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _notificationServiceMock.Setup(x => x.PublishAsync(It.IsAny<PublishNotificationRequest>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var service = CreateService();

        // Act
        var result = await service.CreateOrderAsync(request, CancellationToken.None);

        // Assert
        result.OrderId.Should().Be(2001);
        result.TableId.Should().Be(10);
        result.TableCode.Should().Be("TB-001");
        result.CustomerId.Should().Be(68);
        result.TotalAmount.Should().Be(145_000m);
        result.OrderStatus.Should().Be("PENDING");
        table.TableStatusLvId.Should().Be(OccupiedTableStatusId);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "CreateOrderAsync_Customer")]
    public async Task CreateOrderAsync_Customer_WhenTableOccupied_AddsToExistingOrder()
    {
        // Arrange
        SetupLookupResolver();
        SetupDefaultTax();
        var request = new CreateOrderRequestDTO
        {
            TableCode = "TB-001",
            QrToken = "valid-qr-token",
            Items = new List<CreateOrderItemDTO>
            {
                new() { DishId = 1, Quantity = 1, Price = 50_000m }
            }
        };
        var table = MakeTable(10, "TB-001", OccupiedTableStatusId);
        var existingOrder = MakeOrder(orderId: 500, statusId: PendingStatusId, tableId: 10, customerId: 68);
        existingOrder.TotalAmount = 100_000m;
        _tableRepoMock.Setup(x => x.GetByCodeAsync("TB-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(table);
        _orderRepoMock.Setup(x => x.GetActiveOrderByTableAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingOrder);
        _orderRepoMock.Setup(x => x.AddItemsToOrderAsync(500, It.IsAny<List<OrderItem>>(), It.IsAny<uint>(), It.IsAny<uint>(), It.IsAny<uint>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _uowMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _realtimeMock.Setup(x => x.OrderCreatedAsync(It.IsAny<OrderRealtimeDTO>())).Returns(Task.CompletedTask);
        _shiftLiveMock.Setup(x => x.PublishBoardChangedAsync(It.IsAny<ShiftLiveRealtimeEventDto>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _notificationServiceMock.Setup(x => x.PublishAsync(It.IsAny<PublishNotificationRequest>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var service = CreateService();

        // Act
        var result = await service.CreateOrderAsync(request, CancellationToken.None);

        // Assert
        result.OrderId.Should().Be(500);
        result.TableCode.Should().Be("TB-001");
        result.OrderStatus.Should().Be("PENDING");
        _orderRepoMock.Verify(x => x.AddItemsToOrderAsync(500, It.IsAny<List<OrderItem>>(), It.IsAny<uint>(), It.IsAny<uint>(), It.IsAny<uint>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CreateOrderAsync_Customer")]
    public async Task CreateOrderAsync_Customer_WhenTableNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var request = new CreateOrderRequestDTO
        {
            TableCode = "INVALID",
            Items = new List<CreateOrderItemDTO> { new() { DishId = 1, Quantity = 1, Price = 50_000m } }
        };
        _tableRepoMock.Setup(x => x.GetByCodeAsync("INVALID", It.IsAny<CancellationToken>()))
            .ReturnsAsync((RestaurantTable?)null);
        _uowMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(x => x.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var service = CreateService();

        // Act
        Func<Task> act = () => service.CreateOrderAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Table 'INVALID' not found.");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CreateOrderAsync_Customer")]
    public async Task CreateOrderAsync_Customer_WhenInvalidQrToken_ThrowsValidationException()
    {
        // Arrange
        var request = new CreateOrderRequestDTO
        {
            TableCode = "TB-001",
            QrToken = "wrong-token",
            Items = new List<CreateOrderItemDTO> { new() { DishId = 1, Quantity = 1, Price = 50_000m } }
        };
        var table = MakeTable(10, "TB-001", AvailableTableStatusId);
        table.QrToken = "valid-qr-token";
        _tableRepoMock.Setup(x => x.GetByCodeAsync("TB-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(table);
        _uowMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(x => x.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var service = CreateService();

        // Act
        Func<Task> act = () => service.CreateOrderAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("Invalid QR token. Please scan the QR code on the table again.");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CreateOrderAsync_Customer")]
    public async Task CreateOrderAsync_Customer_WhenTableLocked_ThrowsInvalidOperationException()
    {
        // Arrange
        SetupLookupResolver();
        var request = new CreateOrderRequestDTO
        {
            TableCode = "TB-001",
            Items = new List<CreateOrderItemDTO> { new() { DishId = 1, Quantity = 1, Price = 50_000m } }
        };
        var table = MakeTable(10, "TB-001", LockedTableStatusId);
        table.QrToken = null;
        _tableRepoMock.Setup(x => x.GetByCodeAsync("TB-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(table);
        _uowMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(x => x.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var service = CreateService();

        // Act
        Func<Task> act = () => service.CreateOrderAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Table 'TB-001' is under maintenance and cannot be used.");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CreateOrderAsync_Customer")]
    public async Task CreateOrderAsync_Customer_WhenTableDeleted_ThrowsInvalidOperationException()
    {
        // Arrange
        SetupLookupResolver();
        var request = new CreateOrderRequestDTO
        {
            TableCode = "TB-001",
            Items = new List<CreateOrderItemDTO> { new() { DishId = 1, Quantity = 1, Price = 50_000m } }
        };
        var table = MakeTable(10, "TB-001", AvailableTableStatusId);
        table.IsDeleted = true;
        table.QrToken = null;
        _tableRepoMock.Setup(x => x.GetByCodeAsync("TB-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(table);
        _uowMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(x => x.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var service = CreateService();

        // Act
        Func<Task> act = () => service.CreateOrderAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Table 'TB-001' is no longer available.");
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "CreateOrderAsync_Customer")]
    public async Task CreateOrderAsync_Customer_WhenTableReserved_ChangesToOccupied()
    {
        // Arrange
        SetupLookupResolver();
        SetupDefaultTax();
        var request = new CreateOrderRequestDTO
        {
            TableCode = "TB-001",
            Items = new List<CreateOrderItemDTO>
            {
                new() { DishId = 1, Quantity = 1, Price = 50_000m }
            }
        };
        var table = MakeTable(10, "TB-001", ReservedTableStatusId);
        table.QrToken = null;
        _tableRepoMock.Setup(x => x.GetByCodeAsync("TB-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(table);
        _tableRepoMock.Setup(x => x.UpdateAsync(It.IsAny<RestaurantTable>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _customerServiceMock.Setup(x => x.GetGuestCustomerIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(68L);
        _orderRepoMock.Setup(x => x.CreateOrderAsync(It.IsAny<Order>(), It.IsAny<List<OrderItem>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(2002L);
        _uowMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _realtimeMock.Setup(x => x.OrderCreatedAsync(It.IsAny<OrderRealtimeDTO>())).Returns(Task.CompletedTask);
        _shiftLiveMock.Setup(x => x.PublishBoardChangedAsync(It.IsAny<ShiftLiveRealtimeEventDto>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _notificationServiceMock.Setup(x => x.PublishAsync(It.IsAny<PublishNotificationRequest>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var service = CreateService();

        // Act
        var result = await service.CreateOrderAsync(request, CancellationToken.None);

        // Assert
        table.TableStatusLvId.Should().Be(OccupiedTableStatusId);
        result.OrderId.Should().Be(2002);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "CreateOrderAsync_Customer")]
    public async Task CreateOrderAsync_Customer_WhenTableCodeHasSpaces_TrimsBeforeLookup()
    {
        // Arrange
        var request = new CreateOrderRequestDTO
        {
            TableCode = "  TB-001  ",
            Items = new List<CreateOrderItemDTO> { new() { DishId = 1, Quantity = 1, Price = 50_000m } }
        };
        _tableRepoMock.Setup(x => x.GetByCodeAsync("TB-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync((RestaurantTable?)null);
        _uowMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(x => x.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var service = CreateService();

        // Act
        Func<Task> act = () => service.CreateOrderAsync(request, CancellationToken.None);

        // Assert — verifies Trim() is applied by checking repo is called with trimmed value
        await act.Should().ThrowAsync<KeyNotFoundException>();
        _tableRepoMock.Verify(x => x.GetByCodeAsync("TB-001", It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    // ══════════════════════════════════════════════════════════════════
    // AddItemsToOrderAsync (Customer)
    // ══════════════════════════════════════════════════════════════════

    #region AddItemsToOrderAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "AddItemsToOrderAsync")]
    public async Task AddItemsToOrderAsync_WhenValidOrder_AddsItemsAndNotifies()
    {
        // Arrange
        SetupLookupResolver();
        var order = MakeOrder(orderId: 30, statusId: PendingStatusId, tableId: 10);
        var request = new AddOrderItemsRequestDTO
        {
            Items = new List<CreateOrderItemDTO>
            {
                new() { DishId = 1, Quantity = 2, Price = 50_000m, Note = "Extra spicy" }
            }
        };
        _orderRepoMock.Setup(x => x.GetByIdForUpdateAsync(30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _orderRepoMock.Setup(x => x.AddItemsToOrderAsync(30, It.IsAny<List<OrderItem>>(), CompletedStatusId, CancelledStatusId, PendingStatusId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _dishRepoMock.Setup(x => x.GetByIdsAsync(It.IsAny<List<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Dish> { MakeDish(1, "Pho Bo", 50_000m) });
        _tableRepoMock.Setup(x => x.GetByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeTable(10, "TB-001"));
        _notificationServiceMock.Setup(x => x.PublishAsync(It.IsAny<PublishNotificationRequest>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _taxRepoMock.Setup(x => x.GetDefaultTaxAsync(It.IsAny<CancellationToken>())).ReturnsAsync((Tax?)null);
        _uowMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var service = CreateService();

        // Act
        await service.AddItemsToOrderAsync(30, request, CancellationToken.None);

        // Assert
        _orderRepoMock.Verify(x => x.AddItemsToOrderAsync(30, It.Is<List<OrderItem>>(items =>
            items.Count == 1 &&
            items[0].DishId == 1 &&
            items[0].Quantity == 2 &&
            items[0].Price == 50_000m &&
            items[0].Note == "Extra spicy" &&
            items[0].ItemStatusLvId == CreatedItemStatusId
        ), CompletedStatusId, CancelledStatusId, PendingStatusId, It.IsAny<CancellationToken>()), Times.Once);
        _notificationServiceMock.Verify(x => x.PublishAsync(
            It.Is<PublishNotificationRequest>(n => n.Type == nameof(NotificationType.ORDER_ITEMS_ADDED)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "AddItemsToOrderAsync")]
    public async Task AddItemsToOrderAsync_WhenOrderNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _orderRepoMock.Setup(x => x.GetByIdForUpdateAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);
        var service = CreateService();

        // Act
        Func<Task> act = () => service.AddItemsToOrderAsync(999, new AddOrderItemsRequestDTO { Items = new() }, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Order 999 not found.");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "AddItemsToOrderAsync")]
    public async Task AddItemsToOrderAsync_WhenOrderAlreadyPaid_ThrowsInvalidOperationException()
    {
        // Arrange
        var order = MakeOrder(orderId: 31, statusId: CompletedStatusId);
        order.Payments = new List<Payment> { new() { PaymentId = 1, OrderId = 31, ReceivedAmount = 100_000m } };
        _orderRepoMock.Setup(x => x.GetByIdForUpdateAsync(31, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        var service = CreateService();

        // Act
        Func<Task> act = () => service.AddItemsToOrderAsync(31, new AddOrderItemsRequestDTO { Items = new() }, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("This order has already been paid. Please ask staff to create a new order for your table.");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "AddItemsToOrderAsync")]
    public async Task AddItemsToOrderAsync_WhenDishNotFound_ThrowsNotFoundException()
    {
        // Arrange
        SetupLookupResolver();
        var order = MakeOrder(orderId: 33, statusId: PendingStatusId, tableId: 10);
        var request = new AddOrderItemsRequestDTO
        {
            Items = new List<CreateOrderItemDTO>
            {
                new() { DishId = 1, Quantity = 1, Price = 50_000m },
                new() { DishId = 999, Quantity = 1, Price = 45_000m }
            }
        };

        _orderRepoMock.Setup(x => x.GetByIdForUpdateAsync(33, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _orderRepoMock.Setup(x => x.AddItemsToOrderAsync(33, It.IsAny<List<OrderItem>>(), It.IsAny<uint>(), It.IsAny<uint>(), It.IsAny<uint>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _dishRepoMock.Setup(x => x.GetByIdsAsync(It.IsAny<List<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Dish> { MakeDish(1, "Pho Bo", 50_000m) });

        var service = CreateService();

        // Act
        Func<Task> act = () => service.AddItemsToOrderAsync(33, request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("One or more dishes not found.");
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "AddItemsToOrderAsync")]
    public async Task AddItemsToOrderAsync_NotificationContainsDishNamesAndTableCode()
    {
        // Arrange
        SetupLookupResolver();
        var order = MakeOrder(orderId: 32, statusId: PendingStatusId, tableId: 10);
        var request = new AddOrderItemsRequestDTO
        {
            Items = new List<CreateOrderItemDTO>
            {
                new() { DishId = 1, Quantity = 1, Price = 50_000m },
                new() { DishId = 2, Quantity = 1, Price = 45_000m }
            }
        };
        _orderRepoMock.Setup(x => x.GetByIdForUpdateAsync(32, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _orderRepoMock.Setup(x => x.AddItemsToOrderAsync(32, It.IsAny<List<OrderItem>>(), It.IsAny<uint>(), It.IsAny<uint>(), It.IsAny<uint>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _dishRepoMock.Setup(x => x.GetByIdsAsync(It.IsAny<List<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Dish>
            {
                MakeDish(1, "Pho Bo", 50_000m),
                MakeDish(2, "Bun Cha", 45_000m)
            });
        _tableRepoMock.Setup(x => x.GetByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeTable(10, "TB-001"));
        _notificationServiceMock.Setup(x => x.PublishAsync(It.IsAny<PublishNotificationRequest>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _taxRepoMock.Setup(x => x.GetDefaultTaxAsync(It.IsAny<CancellationToken>())).ReturnsAsync((Tax?)null);
        _uowMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var service = CreateService();

        // Act
        await service.AddItemsToOrderAsync(32, request, CancellationToken.None);

        // Assert
        _notificationServiceMock.Verify(x => x.PublishAsync(
            It.Is<PublishNotificationRequest>(n =>
                n.Metadata != null &&
                (string)n.Metadata["tableCode"] == "TB-001" &&
                ((string)n.Metadata["dishNames"]).Contains("Pho Bo") &&
                ((string)n.Metadata["dishNames"]).Contains("Bun Cha") &&
                (string)n.Metadata["itemCount"] == "2"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    // ══════════════════════════════════════════════════════════════════
    // GetRecentOrdersAsync
    // ══════════════════════════════════════════════════════════════════

    #region GetRecentOrdersAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetRecentOrdersAsync")]
    public async Task GetRecentOrdersAsync_WhenValidInput_ReturnsRecentOrders()
    {
        // Arrange
        var expected = new List<RecentOrderDTO>
        {
            new() { OrderId = 1, CustomerName = "Nguyen Van A", Source = "DINE_IN", TableCode = "TB-001", Status = "PENDING", CreatedAt = new DateTime(2025, 6, 1) },
            new() { OrderId = 2, CustomerName = "Guest", Source = "TAKEAWAY", TableCode = null, Status = "COMPLETED", CreatedAt = new DateTime(2025, 6, 1) }
        };
        _orderRepoMock.Setup(x => x.GetRecentOrdersAsync(5, It.IsAny<List<string>>(), 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var service = CreateService();

        // Act
        var result = await service.GetRecentOrdersAsync(5, new List<string> { "Admin" }, 10, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result[0].OrderId.Should().Be(1);
        result[0].CustomerName.Should().Be("Nguyen Van A");
        result[1].OrderId.Should().Be(2);
        result[1].Source.Should().Be("TAKEAWAY");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetRecentOrdersAsync")]
    public async Task GetRecentOrdersAsync_WhenLimitZero_DefaultsTo20()
    {
        // Arrange
        _orderRepoMock.Setup(x => x.GetRecentOrdersAsync(5, It.IsAny<List<string>>(), 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecentOrderDTO>());
        var service = CreateService();

        // Act
        var result = await service.GetRecentOrdersAsync(5, new List<string> { "Admin" }, 0, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
        _orderRepoMock.Verify(x => x.GetRecentOrdersAsync(5, It.IsAny<List<string>>(), 20, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetRecentOrdersAsync")]
    public async Task GetRecentOrdersAsync_WhenLimitNegative_DefaultsTo20()
    {
        // Arrange
        _orderRepoMock.Setup(x => x.GetRecentOrdersAsync(5, It.IsAny<List<string>>(), 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecentOrderDTO>());
        var service = CreateService();

        // Act
        var result = await service.GetRecentOrdersAsync(5, new List<string> { "Admin" }, -5, CancellationToken.None);

        // Assert
        _orderRepoMock.Verify(x => x.GetRecentOrdersAsync(5, It.IsAny<List<string>>(), 20, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetRecentOrdersAsync")]
    public async Task GetRecentOrdersAsync_WhenLimitExceeds100_DefaultsTo20()
    {
        // Arrange
        _orderRepoMock.Setup(x => x.GetRecentOrdersAsync(5, It.IsAny<List<string>>(), 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecentOrderDTO>());
        var service = CreateService();

        // Act
        var result = await service.GetRecentOrdersAsync(5, new List<string> { "Admin" }, 150, CancellationToken.None);

        // Assert
        _orderRepoMock.Verify(x => x.GetRecentOrdersAsync(5, It.IsAny<List<string>>(), 20, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetRecentOrdersAsync")]
    public async Task GetRecentOrdersAsync_WhenLimit100_UsesExactLimit()
    {
        // Arrange
        _orderRepoMock.Setup(x => x.GetRecentOrdersAsync(5, It.IsAny<List<string>>(), 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecentOrderDTO>());
        var service = CreateService();

        // Act
        await service.GetRecentOrdersAsync(5, new List<string> { "Admin" }, 100, CancellationToken.None);

        // Assert
        _orderRepoMock.Verify(x => x.GetRecentOrdersAsync(5, It.IsAny<List<string>>(), 100, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetRecentOrdersAsync")]
    public async Task GetRecentOrdersAsync_WhenLimit1_UsesExactLimit()
    {
        // Arrange
        _orderRepoMock.Setup(x => x.GetRecentOrdersAsync(5, It.IsAny<List<string>>(), 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecentOrderDTO> { new() { OrderId = 1 } });
        var service = CreateService();

        // Act
        var result = await service.GetRecentOrdersAsync(5, new List<string> { "Admin" }, 1, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        _orderRepoMock.Verify(x => x.GetRecentOrdersAsync(5, It.IsAny<List<string>>(), 1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetRecentOrdersAsync")]
    public async Task GetRecentOrdersAsync_WhenEmptyRoles_PassesEmptyList()
    {
        // Arrange
        var emptyRoles = new List<string>();
        _orderRepoMock.Setup(x => x.GetRecentOrdersAsync(5, emptyRoles, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecentOrderDTO>());
        var service = CreateService();

        // Act
        var result = await service.GetRecentOrdersAsync(5, emptyRoles, 10, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
        _orderRepoMock.Verify(x => x.GetRecentOrdersAsync(5, emptyRoles, 10, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
