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
///               UpdateOrderStatusAsync, UpdateOrderItemStatusAsync, CancelOrderItemAsync,
///               GetOrderByIdAsync, CreateOrderAsync (staff), AddItemsAsync,
///               GetCustomerOrderByIdAsync, AddItemsToOrderAsync (customer),
///               CreateOrderAsync (customer DTO), GetRecentOrdersAsync
/// Created By  : quantm
/// Executed By : quantm
/// Test Req.   : Kiểm tra toàn bộ logic quản lý đơn hàng bao gồm:
///               CRUD đơn hàng, chuyển trạng thái, thêm món, huỷ món,
///               xử lý bàn khi đổi trạng thái, validate thanh toán,
///               tạo đơn từ QR (customer), tính thuế tự động.
/// </summary>
public class OrderServiceTests
{
    // ── Mocks ──────────────────────────────────────────────────────────────
    private readonly Mock<IOrderRepository>              _orderRepoMock    = new();
    private readonly Mock<ITableRepository>              _tableRepoMock    = new();
    private readonly Mock<ILookupResolver>               _lookupResolverMock = new();
    private readonly Mock<IDishRepository>               _dishRepoMock     = new();
    private readonly Mock<ICustomerService>              _customerServiceMock = new();
    private readonly Mock<IUnitOfWork>                   _uowMock          = new();
    private readonly Mock<INotificationService>          _notificationServiceMock = new();
    private readonly Mock<IOrderRealtimeService>         _realtimeMock     = new();
    private readonly Mock<IShiftLiveRealtimePublisher>   _shiftLiveMock    = new();
    private readonly Mock<ITaxRepository>                _taxRepoMock      = new();

    // ── Lookup ID Constants ────────────────────────────────────────────────
    private const uint ORDER_PENDING_ID     = 10;
    private const uint ORDER_IN_PROGRESS_ID = 11;
    private const uint ORDER_COMPLETED_ID   = 12;
    private const uint ORDER_CANCELLED_ID   = 13;

    private const uint ITEM_CREATED_ID      = 20;
    private const uint ITEM_IN_PROGRESS_ID  = 21;
    private const uint ITEM_READY_ID        = 22;
    private const uint ITEM_SERVED_ID       = 23;
    private const uint ITEM_REJECTED_ID     = 24;
    private const uint ITEM_CANCELLED_ID    = 25;

    private const uint TABLE_AVAILABLE_ID   = 30;
    private const uint TABLE_OCCUPIED_ID    = 31;
    private const uint TABLE_LOCKED_ID      = 32;
    private const uint TABLE_RESERVED_ID    = 33;

    private const uint SOURCE_DINE_IN_ID    = 40;
    private const uint SOURCE_TAKEAWAY_ID   = 41;

    // ── Factory ────────────────────────────────────────────────────────────
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

    // ── Test Data Helpers ──────────────────────────────────────────────────
    private static Order MakeOrder(
        long orderId = 1,
        long? tableId = 1,
        long? staffId = 100,
        uint statusId = ORDER_PENDING_ID,
        decimal totalAmount = 100m,
        List<OrderItem>? items = null,
        List<Payment>? payments = null) => new()
    {
        OrderId = orderId,
        TableId = tableId,
        StaffId = staffId,
        CustomerId = 1,
        TotalAmount = totalAmount,
        SubTotalAmount = totalAmount,
        OrderStatusLvId = statusId,
        SourceLvId = SOURCE_DINE_IN_ID,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        OrderStatusLv = new LookupValue { ValueCode = statusId switch
        {
            ORDER_PENDING_ID => "PENDING",
            ORDER_IN_PROGRESS_ID => "IN_PROGRESS",
            ORDER_COMPLETED_ID => "COMPLETED",
            ORDER_CANCELLED_ID => "CANCELLED",
            _ => "PENDING"
        }},
        OrderItems = items != null ? new List<OrderItem>(items) : new List<OrderItem>(),
        Payments = payments != null ? new List<Payment>(payments) : new List<Payment>()
    };

    private static RestaurantTable MakeTable(
        long tableId = 1,
        string tableCode = "T001",
        int capacity = 4,
        uint statusId = TABLE_AVAILABLE_ID) => new()
    {
        TableId = tableId,
        TableCode = tableCode,
        Capacity = capacity,
        TableStatusLvId = statusId,
        IsOnline = true,
        QrToken = "valid-qr-token"
    };

    private static Dish MakeDish(long dishId = 1, string name = "Phở bò", decimal price = 50000m) => new()
    {
        DishId = dishId,
        DishName = name,
        Price = price
    };

    private static Tax MakeTax(long taxId = 1, decimal rate = 10m, string type = "EXCLUSIVE", bool isDefault = true) => new()
    {
        TaxId = taxId,
        TaxName = "VAT",
        TaxRate = rate,
        TaxType = type,
        IsActive = true,
        IsDefault = isDefault
    };

    // ── Common Setup Helpers ───────────────────────────────────────────────
    private void SetupDefaultLookupBehavior()
    {
        _lookupResolverMock
            .Setup(r => r.GetIdAsync(It.IsAny<ushort>(), It.IsAny<System.Enum>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ushort typeId, System.Enum code, CancellationToken ct) =>
            {
                return code switch
                {
                    OrderStatusCode.PENDING       => ORDER_PENDING_ID,
                    OrderStatusCode.IN_PROGRESS   => ORDER_IN_PROGRESS_ID,
                    OrderStatusCode.COMPLETED     => ORDER_COMPLETED_ID,
                    OrderStatusCode.CANCELLED     => ORDER_CANCELLED_ID,
                    OrderItemStatusCode.CREATED   => ITEM_CREATED_ID,
                    OrderItemStatusCode.IN_PROGRESS => ITEM_IN_PROGRESS_ID,
                    OrderItemStatusCode.READY     => ITEM_READY_ID,
                    OrderItemStatusCode.SERVED    => ITEM_SERVED_ID,
                    OrderItemStatusCode.REJECTED  => ITEM_REJECTED_ID,
                    OrderItemStatusCode.CANCELLED => ITEM_CANCELLED_ID,
                    TableStatusCode.AVAILABLE     => TABLE_AVAILABLE_ID,
                    TableStatusCode.OCCUPIED      => TABLE_OCCUPIED_ID,
                    TableStatusCode.LOCKED        => TABLE_LOCKED_ID,
                    TableStatusCode.RESERVED      => TABLE_RESERVED_ID,
                    OrderSourceCode.DINE_IN       => SOURCE_DINE_IN_ID,
                    OrderSourceCode.TAKEAWAY      => SOURCE_TAKEAWAY_ID,
                    _ => 0u
                };
            });
    }

    private void SetupDefaultUoW()
    {
        _uowMock.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(u => u.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
    }

    private void SetupNoDefaultTax()
    {
        _taxRepoMock
            .Setup(t => t.GetDefaultTaxAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tax?)null);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TEST CASES — GetOrderHistoryAsync
    // ═══════════════════════════════════════════════════════════════════════

    #region GetOrderHistoryAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetOrderHistoryAsync")]
    public async Task GetOrderHistoryAsync_WhenDataExists_ReturnsPagedResult()
    {
        // Arrange
        var expected = new PagedResultDTO<OrderHistoryDTO>
        {
            PageData = new List<OrderHistoryDTO>
            {
                new() { OrderId = 1 },
                new() { OrderId = 2 }
            },
            TotalCount = 2
        };

        _orderRepoMock
            .Setup(r => r.GetOrderHistoryAsync(It.IsAny<OrderHistoryQueryDTO>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var service = CreateService();
        var query = new OrderHistoryQueryDTO();

        // Act
        var result = await service.GetOrderHistoryAsync(query);

        // Assert
        result.PageData.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetOrderHistoryAsync")]
    public async Task GetOrderHistoryAsync_WhenNoData_ReturnsEmptyResult()
    {
        // Arrange
        var expected = new PagedResultDTO<OrderHistoryDTO>
        {
            PageData = new List<OrderHistoryDTO>(),
            TotalCount = 0
        };

        _orderRepoMock
            .Setup(r => r.GetOrderHistoryAsync(It.IsAny<OrderHistoryQueryDTO>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var service = CreateService();
        var query = new OrderHistoryQueryDTO();

        // Act
        var result = await service.GetOrderHistoryAsync(query);

        // Assert
        result.PageData.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "GetOrderHistoryAsync")]
    public async Task GetOrderHistoryAsync_WhenRepositoryThrows_PropagatesException()
    {
        // Arrange
        _orderRepoMock
            .Setup(r => r.GetOrderHistoryAsync(It.IsAny<OrderHistoryQueryDTO>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Invalid query parameters"));

        var service = CreateService();

        // Act & Assert
        await service.Invoking(s => s.GetOrderHistoryAsync(new OrderHistoryQueryDTO()))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Invalid query*");
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════
    // TEST CASES — GetOrderStatusCountAsync
    // ═══════════════════════════════════════════════════════════════════════

    #region GetOrderStatusCountAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetOrderStatusCountAsync")]
    public async Task GetOrderStatusCountAsync_WhenCalled_ResolvesStatusIdsAndReturnsCount()
    {
        // Arrange
        SetupDefaultLookupBehavior();

        var expected = new OrderStatusCountDTO
        {
            Pending = 5,
            InProgress = 3,
            Completed = 10,
            Cancelled = 1
        };

        _orderRepoMock
            .Setup(r => r.GetOrderStatusCountAsync(
                ORDER_PENDING_ID, ORDER_IN_PROGRESS_ID, ORDER_COMPLETED_ID, ORDER_CANCELLED_ID,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var service = CreateService();

        // Act
        var result = await service.GetOrderStatusCountAsync();

        // Assert
        result.Pending.Should().Be(5);
        result.Completed.Should().Be(10);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetOrderStatusCountAsync")]
    public async Task GetOrderStatusCountAsync_WhenAllZero_ReturnsZeroCounts()
    {
        // Arrange
        SetupDefaultLookupBehavior();

        _orderRepoMock
            .Setup(r => r.GetOrderStatusCountAsync(
                It.IsAny<uint>(), It.IsAny<uint>(), It.IsAny<uint>(), It.IsAny<uint>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OrderStatusCountDTO());

        var service = CreateService();

        // Act
        var result = await service.GetOrderStatusCountAsync();

        // Assert
        result.Pending.Should().Be(0);
        result.InProgress.Should().Be(0);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "GetOrderStatusCountAsync")]
    public async Task GetOrderStatusCountAsync_WhenLookupResolverFails_PropagatesException()
    {
        // Arrange — lookup resolver cannot resolve status codes (corrupt/missing data)
        _lookupResolverMock
            .Setup(r => r.GetIdAsync(It.IsAny<ushort>(), It.IsAny<System.Enum>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("Lookup value not found"));

        var service = CreateService();

        // Act & Assert
        await service.Invoking(s => s.GetOrderStatusCountAsync())
            .Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*not found*");
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════
    // TEST CASES — GetKitchenOrdersAsync
    // ═══════════════════════════════════════════════════════════════════════

    #region GetKitchenOrdersAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetKitchenOrdersAsync")]
    public async Task GetKitchenOrdersAsync_WhenOrdersExist_ReturnsList()
    {
        // Arrange
        SetupDefaultLookupBehavior();

        var orders = new List<KitchenOrderDTO>
        {
            new() { OrderId = 1 },
            new() { OrderId = 2 }
        };

        _orderRepoMock
            .Setup(r => r.GetKitchenOrdersAsync(
                ORDER_PENDING_ID, ORDER_IN_PROGRESS_ID, ORDER_COMPLETED_ID, ORDER_CANCELLED_ID,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        var service = CreateService();

        // Act
        var result = await service.GetKitchenOrdersAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetKitchenOrdersAsync")]
    public async Task GetKitchenOrdersAsync_WhenNoOrders_ReturnsEmpty()
    {
        // Arrange
        SetupDefaultLookupBehavior();

        _orderRepoMock
            .Setup(r => r.GetKitchenOrdersAsync(
                It.IsAny<uint>(), It.IsAny<uint>(), It.IsAny<uint>(), It.IsAny<uint>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<KitchenOrderDTO>());

        var service = CreateService();

        // Act
        var result = await service.GetKitchenOrdersAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "GetKitchenOrdersAsync")]
    public async Task GetKitchenOrdersAsync_WhenLookupResolverFails_PropagatesException()
    {
        // Arrange — lookup resolver cannot resolve status codes (corrupt/missing data)
        _lookupResolverMock
            .Setup(r => r.GetIdAsync(It.IsAny<ushort>(), It.IsAny<System.Enum>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("Lookup value not found"));

        var service = CreateService();

        // Act & Assert
        await service.Invoking(s => s.GetKitchenOrdersAsync())
            .Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*not found*");
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════
    // TEST CASES — UpdateOrderStatusAsync
    // ═══════════════════════════════════════════════════════════════════════

    #region UpdateOrderStatusAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "UpdateOrderStatusAsync")]
    public async Task UpdateOrderStatusAsync_WhenPendingToInProgress_UpdatesStatusAndCommits()
    {
        // Arrange
        SetupDefaultLookupBehavior();
        SetupDefaultUoW();
        SetupNoDefaultTax();

        var order = MakeOrder(statusId: ORDER_PENDING_ID);

        _orderRepoMock
            .Setup(r => r.GetByIdForUpdateAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var service = CreateService();

        // Act
        await service.UpdateOrderStatusAsync(1, OrderStatusCode.IN_PROGRESS);

        // Assert
        order.OrderStatusLvId.Should().Be(ORDER_IN_PROGRESS_ID);
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "UpdateOrderStatusAsync")]
    public async Task UpdateOrderStatusAsync_WhenCancelled_SetsTableToAvailable()
    {
        // Arrange
        SetupDefaultLookupBehavior();
        SetupDefaultUoW();
        SetupNoDefaultTax();

        var order = MakeOrder(statusId: ORDER_IN_PROGRESS_ID, tableId: 1);
        var table = MakeTable(1, "T001", 4, TABLE_OCCUPIED_ID);

        _orderRepoMock
            .Setup(r => r.GetByIdForUpdateAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _tableRepoMock
            .Setup(t => t.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(table);

        var service = CreateService();

        // Act
        await service.UpdateOrderStatusAsync(1, OrderStatusCode.CANCELLED);

        // Assert
        table.TableStatusLvId.Should().Be(TABLE_AVAILABLE_ID);
        _tableRepoMock.Verify(t => t.UpdateAsync(table, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "UpdateOrderStatusAsync")]
    public async Task UpdateOrderStatusAsync_WhenCompleted_SetsTableToAvailable()
    {
        // Arrange
        SetupDefaultLookupBehavior();
        SetupDefaultUoW();
        SetupNoDefaultTax();

        var order = MakeOrder(statusId: ORDER_IN_PROGRESS_ID, tableId: 1);
        var table = MakeTable(1, "T001", 4, TABLE_OCCUPIED_ID);

        _orderRepoMock
            .Setup(r => r.GetByIdForUpdateAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _tableRepoMock
            .Setup(t => t.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(table);

        var service = CreateService();

        // Act
        await service.UpdateOrderStatusAsync(1, OrderStatusCode.COMPLETED);

        // Assert
        table.TableStatusLvId.Should().Be(TABLE_AVAILABLE_ID);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "UpdateOrderStatusAsync")]
    public async Task UpdateOrderStatusAsync_WhenCancelledToPending_SetsTableToOccupied()
    {
        // Arrange
        SetupDefaultLookupBehavior();
        SetupDefaultUoW();
        SetupNoDefaultTax();

        var order = MakeOrder(statusId: ORDER_CANCELLED_ID, tableId: 1);
        var table = MakeTable(1, "T001", 4, TABLE_AVAILABLE_ID);

        _orderRepoMock
            .Setup(r => r.GetByIdForUpdateAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _tableRepoMock
            .Setup(t => t.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(table);

        var service = CreateService();

        // Act
        await service.UpdateOrderStatusAsync(1, OrderStatusCode.PENDING);

        // Assert
        table.TableStatusLvId.Should().Be(TABLE_OCCUPIED_ID);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "UpdateOrderStatusAsync")]
    public async Task UpdateOrderStatusAsync_WhenOrderNotFound_ThrowsNotFoundException()
    {
        // Arrange
        SetupDefaultLookupBehavior();

        _orderRepoMock
            .Setup(r => r.GetByIdForUpdateAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var service = CreateService();

        // Act & Assert
        await service.Invoking(s => s.UpdateOrderStatusAsync(999, OrderStatusCode.IN_PROGRESS))
            .Should().ThrowAsync<NotFoundException>()
            .WithMessage("*not found*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "UpdateOrderStatusAsync")]
    public async Task UpdateOrderStatusAsync_WhenSameStatus_ThrowsInvalidOperationException()
    {
        // Arrange
        SetupDefaultLookupBehavior();

        var order = MakeOrder(statusId: ORDER_PENDING_ID);

        _orderRepoMock
            .Setup(r => r.GetByIdForUpdateAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var service = CreateService();

        // Act & Assert
        await service.Invoking(s => s.UpdateOrderStatusAsync(1, OrderStatusCode.PENDING))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already in this status*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "UpdateOrderStatusAsync")]
    public async Task UpdateOrderStatusAsync_WhenInvalidTransition_ThrowsInvalidOperationException()
    {
        // Arrange
        SetupDefaultLookupBehavior();

        var order = MakeOrder(statusId: ORDER_PENDING_ID);

        _orderRepoMock
            .Setup(r => r.GetByIdForUpdateAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var service = CreateService();

        // Act & Assert — PENDING → COMPLETED is not allowed
        await service.Invoking(s => s.UpdateOrderStatusAsync(1, OrderStatusCode.COMPLETED))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Invalid status transition*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "UpdateOrderStatusAsync")]
    public async Task UpdateOrderStatusAsync_WhenCancelPaidOrder_ThrowsInvalidOperationException()
    {
        // Arrange
        SetupDefaultLookupBehavior();

        var payment = new Payment { PaymentId = 1, ReceivedAmount = 100m };
        var order = MakeOrder(statusId: ORDER_IN_PROGRESS_ID, payments: new List<Payment> { payment });

        _orderRepoMock
            .Setup(r => r.GetByIdForUpdateAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var service = CreateService();

        // Act & Assert
        await service.Invoking(s => s.UpdateOrderStatusAsync(1, OrderStatusCode.CANCELLED))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*paid order*");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "UpdateOrderStatusAsync")]
    public async Task UpdateOrderStatusAsync_WhenNoTableId_SkipsTableUpdate()
    {
        // Arrange
        SetupDefaultLookupBehavior();
        SetupDefaultUoW();
        SetupNoDefaultTax();

        var order = MakeOrder(statusId: ORDER_PENDING_ID, tableId: null);

        _orderRepoMock
            .Setup(r => r.GetByIdForUpdateAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var service = CreateService();

        // Act
        await service.UpdateOrderStatusAsync(1, OrderStatusCode.IN_PROGRESS);

        // Assert
        _tableRepoMock.Verify(
            t => t.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "UpdateOrderStatusAsync")]
    public async Task UpdateOrderStatusAsync_WhenCancelled_SendsNotification()
    {
        // Arrange
        SetupDefaultLookupBehavior();
        SetupDefaultUoW();
        SetupNoDefaultTax();

        var order = MakeOrder(statusId: ORDER_IN_PROGRESS_ID, tableId: null);

        _orderRepoMock
            .Setup(r => r.GetByIdForUpdateAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var service = CreateService();

        // Act
        await service.UpdateOrderStatusAsync(1, OrderStatusCode.CANCELLED);

        // Assert
        _notificationServiceMock.Verify(
            n => n.PublishAsync(It.Is<PublishNotificationRequest>(r => r.Type == nameof(NotificationType.ORDER_CANCELLED)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════
    // TEST CASES — CreateOrderAsync (staff)
    // ═══════════════════════════════════════════════════════════════════════

    #region CreateOrderAsync_Staff

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "CreateOrderAsync_Staff")]
    public async Task CreateOrderAsync_Staff_WhenDineInValid_CreatesOrderAndSetsTableOccupied()
    {
        // Arrange
        SetupDefaultLookupBehavior();
        SetupDefaultUoW();
        SetupNoDefaultTax();

        var table = MakeTable(1, "T001", 4, TABLE_AVAILABLE_ID);
        var dish = MakeDish(1, "Phở bò", 50000m);

        _tableRepoMock
            .Setup(t => t.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(table);

        _dishRepoMock
            .Setup(d => d.GetByIdsAsync(It.IsAny<List<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Dish> { dish });

        _customerServiceMock
            .Setup(c => c.ResolveCustomerAsync(It.IsAny<OrderCustomerDto?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1L);

        _orderRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Callback<Order, CancellationToken>((o, _) => o.OrderId = 100);

        var service = CreateService();
        var request = new CreateOrderRequest
        {
            TableId = 1,
            Source = OrderSourceCode.DINE_IN,
            Items = new List<CreateOrderItemDto>
            {
                new() { DishId = 1, Quantity = 2 }
            }
        };

        // Act
        var result = await service.CreateOrderAsync(1, request, default);

        // Assert
        result.Should().Be(100);
        table.TableStatusLvId.Should().Be(TABLE_OCCUPIED_ID);
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "CreateOrderAsync_Staff")]
    public async Task CreateOrderAsync_Staff_WhenTakeaway_NoTableUpdate()
    {
        // Arrange
        SetupDefaultLookupBehavior();
        SetupDefaultUoW();
        SetupNoDefaultTax();

        var dish = MakeDish(1, "Phở bò", 50000m);

        _dishRepoMock
            .Setup(d => d.GetByIdsAsync(It.IsAny<List<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Dish> { dish });

        _customerServiceMock
            .Setup(c => c.ResolveCustomerAsync(It.IsAny<OrderCustomerDto?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1L);

        _orderRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Callback<Order, CancellationToken>((o, _) => o.OrderId = 101);

        var service = CreateService();
        var request = new CreateOrderRequest
        {
            TableId = null,
            Source = OrderSourceCode.TAKEAWAY,
            Items = new List<CreateOrderItemDto>
            {
                new() { DishId = 1, Quantity = 1 }
            }
        };

        // Act
        var result = await service.CreateOrderAsync(1, request, default);

        // Assert
        result.Should().Be(101);
        _tableRepoMock.Verify(
            t => t.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CreateOrderAsync_Staff")]
    public async Task CreateOrderAsync_Staff_WhenNoItems_ThrowsInvalidOperationException()
    {
        // Arrange
        var service = CreateService();
        var request = new CreateOrderRequest
        {
            TableId = 1,
            Source = OrderSourceCode.DINE_IN,
            Items = new List<CreateOrderItemDto>()
        };

        // Act & Assert
        await service.Invoking(s => s.CreateOrderAsync(1, request, default))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*at least one item*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CreateOrderAsync_Staff")]
    public async Task CreateOrderAsync_Staff_WhenDineInNoTable_ThrowsInvalidOperationException()
    {
        // Arrange
        var service = CreateService();
        var request = new CreateOrderRequest
        {
            TableId = null,
            Source = OrderSourceCode.DINE_IN,
            Items = new List<CreateOrderItemDto>
            {
                new() { DishId = 1, Quantity = 1 }
            }
        };

        // Act & Assert
        await service.Invoking(s => s.CreateOrderAsync(1, request, default))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*requires table*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CreateOrderAsync_Staff")]
    public async Task CreateOrderAsync_Staff_WhenTakeawayWithTable_ThrowsInvalidOperationException()
    {
        // Arrange
        var service = CreateService();
        var request = new CreateOrderRequest
        {
            TableId = 1,
            Source = OrderSourceCode.TAKEAWAY,
            Items = new List<CreateOrderItemDto>
            {
                new() { DishId = 1, Quantity = 1 }
            }
        };

        // Act & Assert
        await service.Invoking(s => s.CreateOrderAsync(1, request, default))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*cannot have table*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CreateOrderAsync_Staff")]
    public async Task CreateOrderAsync_Staff_WhenTableNotFound_ThrowsNotFoundException()
    {
        // Arrange
        SetupDefaultLookupBehavior();
        SetupDefaultUoW();

        _tableRepoMock
            .Setup(t => t.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RestaurantTable?)null);

        var service = CreateService();
        var request = new CreateOrderRequest
        {
            TableId = 999,
            Source = OrderSourceCode.DINE_IN,
            Items = new List<CreateOrderItemDto>
            {
                new() { DishId = 1, Quantity = 1 }
            }
        };

        // Act & Assert
        await service.Invoking(s => s.CreateOrderAsync(1, request, default))
            .Should().ThrowAsync<NotFoundException>()
            .WithMessage("*not found*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CreateOrderAsync_Staff")]
    public async Task CreateOrderAsync_Staff_WhenTableOccupied_ThrowsInvalidOperationException()
    {
        // Arrange
        SetupDefaultLookupBehavior();
        SetupDefaultUoW();

        var occupiedTable = MakeTable(1, "T001", 4, TABLE_OCCUPIED_ID);

        _tableRepoMock
            .Setup(t => t.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(occupiedTable);

        var service = CreateService();
        var request = new CreateOrderRequest
        {
            TableId = 1,
            Source = OrderSourceCode.DINE_IN,
            Items = new List<CreateOrderItemDto>
            {
                new() { DishId = 1, Quantity = 1 }
            }
        };

        // Act & Assert
        await service.Invoking(s => s.CreateOrderAsync(1, request, default))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not available*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CreateOrderAsync_Staff")]
    public async Task CreateOrderAsync_Staff_WhenDishNotFound_ThrowsNotFoundException()
    {
        // Arrange
        SetupDefaultLookupBehavior();
        SetupDefaultUoW();
        SetupNoDefaultTax();

        var table = MakeTable(1, "T001", 4, TABLE_AVAILABLE_ID);

        _tableRepoMock
            .Setup(t => t.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(table);

        _dishRepoMock
            .Setup(d => d.GetByIdsAsync(It.IsAny<List<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Dish>()); // no dishes found

        _customerServiceMock
            .Setup(c => c.ResolveCustomerAsync(It.IsAny<OrderCustomerDto?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1L);

        var service = CreateService();
        var request = new CreateOrderRequest
        {
            TableId = 1,
            Source = OrderSourceCode.DINE_IN,
            Items = new List<CreateOrderItemDto>
            {
                new() { DishId = 999, Quantity = 1 }
            }
        };

        // Act & Assert
        await service.Invoking(s => s.CreateOrderAsync(1, request, default))
            .Should().ThrowAsync<NotFoundException>()
            .WithMessage("*dishes not found*");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "CreateOrderAsync_Staff")]
    public async Task CreateOrderAsync_Staff_WhenOnFailure_RollsBack()
    {
        // Arrange
        SetupDefaultLookupBehavior();
        SetupDefaultUoW();

        var table = MakeTable(1, "T001", 4, TABLE_AVAILABLE_ID);

        _tableRepoMock
            .Setup(t => t.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(table);

        _dishRepoMock
            .Setup(d => d.GetByIdsAsync(It.IsAny<List<long>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB failure"));

        _customerServiceMock
            .Setup(c => c.ResolveCustomerAsync(It.IsAny<OrderCustomerDto?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1L);

        var service = CreateService();
        var request = new CreateOrderRequest
        {
            TableId = 1,
            Source = OrderSourceCode.DINE_IN,
            Items = new List<CreateOrderItemDto>
            {
                new() { DishId = 1, Quantity = 1 }
            }
        };

        // Act & Assert
        await service.Invoking(s => s.CreateOrderAsync(1, request, default))
            .Should().ThrowAsync<Exception>();

        _uowMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════
    // TEST CASES — AddItemsAsync (staff)
    // ═══════════════════════════════════════════════════════════════════════

    #region AddItemsAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "AddItemsAsync")]
    public async Task AddItemsAsync_WhenValidItems_AddsItemsAndUpdatesTotal()
    {
        // Arrange
        SetupDefaultLookupBehavior();
        SetupDefaultUoW();
        SetupNoDefaultTax();

        var existingOrder = MakeOrder(statusId: ORDER_IN_PROGRESS_ID, totalAmount: 100000m);
        existingOrder.OrderStatusLv = new LookupValue { ValueCode = "IN_PROGRESS" };

        _orderRepoMock
            .Setup(r => r.GetByIdForUpdateAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingOrder);

        var dish = MakeDish(1, "Phở bò", 50000m);

        _dishRepoMock
            .Setup(d => d.GetByIdsAsync(It.IsAny<List<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Dish> { dish });

        var service = CreateService();
        var request = new AddOrderItemsRequest
        {
            Items = new List<CreateOrderItemDto>
            {
                new() { DishId = 1, Quantity = 2 }
            }
        };

        // Act
        await service.AddItemsAsync(1, request, default);

        // Assert
        existingOrder.TotalAmount.Should().Be(200000m); // 100000 + 50000*2
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "AddItemsAsync")]
    public async Task AddItemsAsync_WhenOrderNotFound_ThrowsNotFoundException()
    {
        // Arrange
        SetupDefaultLookupBehavior();
        SetupDefaultUoW();

        _orderRepoMock
            .Setup(r => r.GetByIdForUpdateAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var service = CreateService();
        var request = new AddOrderItemsRequest
        {
            Items = new List<CreateOrderItemDto>
            {
                new() { DishId = 1, Quantity = 1 }
            }
        };

        // Act & Assert
        await service.Invoking(s => s.AddItemsAsync(999, request, default))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "AddItemsAsync")]
    public async Task AddItemsAsync_WhenOrderCancelled_ThrowsInvalidOperationException()
    {
        // Arrange
        SetupDefaultLookupBehavior();
        SetupDefaultUoW();

        var cancelledOrder = MakeOrder(statusId: ORDER_CANCELLED_ID);

        _orderRepoMock
            .Setup(r => r.GetByIdForUpdateAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cancelledOrder);

        var service = CreateService();
        var request = new AddOrderItemsRequest
        {
            Items = new List<CreateOrderItemDto>
            {
                new() { DishId = 1, Quantity = 1 }
            }
        };

        // Act & Assert
        await service.Invoking(s => s.AddItemsAsync(1, request, default))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*canceled order*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "AddItemsAsync")]
    public async Task AddItemsAsync_WhenOrderHasPayment_ThrowsInvalidOperationException()
    {
        // Arrange
        SetupDefaultLookupBehavior();
        SetupDefaultUoW();

        var paidOrder = MakeOrder(statusId: ORDER_IN_PROGRESS_ID,
            payments: new List<Payment> { new() { PaymentId = 1, ReceivedAmount = 100m } });

        _orderRepoMock
            .Setup(r => r.GetByIdForUpdateAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paidOrder);

        var service = CreateService();
        var request = new AddOrderItemsRequest
        {
            Items = new List<CreateOrderItemDto>
            {
                new() { DishId = 1, Quantity = 1 }
            }
        };

        // Act & Assert
        await service.Invoking(s => s.AddItemsAsync(1, request, default))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*paid order*");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "AddItemsAsync")]
    public async Task AddItemsAsync_WhenCompletedOrder_SetsStatusToInProgress()
    {
        // Arrange
        SetupDefaultLookupBehavior();
        SetupDefaultUoW();
        SetupNoDefaultTax();

        var completedOrder = MakeOrder(statusId: ORDER_COMPLETED_ID, totalAmount: 100000m);
        completedOrder.OrderStatusLv = new LookupValue { ValueCode = "COMPLETED" };

        _orderRepoMock
            .Setup(r => r.GetByIdForUpdateAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(completedOrder);

        var dish = MakeDish(1, "Phở bò", 50000m);

        _dishRepoMock
            .Setup(d => d.GetByIdsAsync(It.IsAny<List<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Dish> { dish });

        var service = CreateService();
        var request = new AddOrderItemsRequest
        {
            Items = new List<CreateOrderItemDto>
            {
                new() { DishId = 1, Quantity = 1 }
            }
        };

        // Act
        await service.AddItemsAsync(1, request, default);

        // Assert — completed → in_progress when adding items
        completedOrder.OrderStatusLvId.Should().Be(ORDER_IN_PROGRESS_ID);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "AddItemsAsync")]
    public async Task AddItemsAsync_WhenDishNotFound_ThrowsNotFoundException()
    {
        // Arrange
        SetupDefaultLookupBehavior();
        SetupDefaultUoW();

        var order = MakeOrder(statusId: ORDER_IN_PROGRESS_ID);
        order.OrderStatusLv = new LookupValue { ValueCode = "IN_PROGRESS" };

        _orderRepoMock
            .Setup(r => r.GetByIdForUpdateAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _dishRepoMock
            .Setup(d => d.GetByIdsAsync(It.IsAny<List<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Dish>()); // empty

        var service = CreateService();
        var request = new AddOrderItemsRequest
        {
            Items = new List<CreateOrderItemDto>
            {
                new() { DishId = 999, Quantity = 1 }
            }
        };

        // Act & Assert
        await service.Invoking(s => s.AddItemsAsync(1, request, default))
            .Should().ThrowAsync<NotFoundException>()
            .WithMessage("*dishes not found*");
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════
    // TEST CASES — CreateOrderAsync (customer DTO)
    // ═══════════════════════════════════════════════════════════════════════

    #region CreateOrderAsync_Customer

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "CreateOrderAsync_Customer")]
    public async Task CreateOrderAsync_Customer_WhenValid_CreatesOrderAndReturnsResponse()
    {
        // Arrange
        SetupDefaultLookupBehavior();
        SetupDefaultUoW();
        SetupNoDefaultTax();

        var table = MakeTable(1, "T001", 4, TABLE_AVAILABLE_ID);

        _tableRepoMock
            .Setup(t => t.GetByCodeAsync("T001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(table);

        _customerServiceMock
            .Setup(c => c.GetGuestCustomerIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(68L);

        _orderRepoMock
            .Setup(r => r.CreateOrderAsync(It.IsAny<Order>(), It.IsAny<List<OrderItem>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(200L);

        var service = CreateService();
        var request = new CreateOrderRequestDTO
        {
            TableCode = "T001",
            QrToken = "valid-qr-token",
            Items = new List<CreateOrderItemDTO>
            {
                new() { DishId = 1, Quantity = 2, Price = 50000m }
            }
        };

        // Act
        var result = await service.CreateOrderAsync(request);

        // Assert
        result.OrderId.Should().Be(200);
        result.TableCode.Should().Be("T001");
        result.CustomerId.Should().Be(68);
        result.OrderStatus.Should().Be("PENDING");
        table.TableStatusLvId.Should().Be(TABLE_OCCUPIED_ID);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CreateOrderAsync_Customer")]
    public async Task CreateOrderAsync_Customer_WhenTableNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        SetupDefaultUoW();

        _tableRepoMock
            .Setup(t => t.GetByCodeAsync("INVALID", It.IsAny<CancellationToken>()))
            .ReturnsAsync((RestaurantTable?)null);

        var service = CreateService();
        var request = new CreateOrderRequestDTO
        {
            TableCode = "INVALID",
            Items = new List<CreateOrderItemDTO>
            {
                new() { DishId = 1, Quantity = 1, Price = 50000m }
            }
        };

        // Act & Assert
        await service.Invoking(s => s.CreateOrderAsync(request))
            .Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*not found*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CreateOrderAsync_Customer")]
    public async Task CreateOrderAsync_Customer_WhenInvalidQrToken_ThrowsValidationException()
    {
        // Arrange
        SetupDefaultUoW();

        var table = MakeTable(1, "T001", 4, TABLE_AVAILABLE_ID);
        table.QrToken = "correct-token";

        _tableRepoMock
            .Setup(t => t.GetByCodeAsync("T001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(table);

        var service = CreateService();
        var request = new CreateOrderRequestDTO
        {
            TableCode = "T001",
            QrToken = "wrong-token",
            Items = new List<CreateOrderItemDTO>
            {
                new() { DishId = 1, Quantity = 1, Price = 50000m }
            }
        };

        // Act & Assert
        await service.Invoking(s => s.CreateOrderAsync(request))
            .Should().ThrowAsync<ValidationException>()
            .WithMessage("*Invalid QR token*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CreateOrderAsync_Customer")]
    public async Task CreateOrderAsync_Customer_WhenTableLocked_ThrowsInvalidOperationException()
    {
        // Arrange
        SetupDefaultLookupBehavior();
        SetupDefaultUoW();

        var lockedTable = MakeTable(1, "T001", 4, TABLE_LOCKED_ID);

        _tableRepoMock
            .Setup(t => t.GetByCodeAsync("T001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(lockedTable);

        var service = CreateService();
        var request = new CreateOrderRequestDTO
        {
            TableCode = "T001",
            Items = new List<CreateOrderItemDTO>
            {
                new() { DishId = 1, Quantity = 1, Price = 50000m }
            }
        };

        // Act & Assert
        await service.Invoking(s => s.CreateOrderAsync(request))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*maintenance*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CreateOrderAsync_Customer")]
    public async Task CreateOrderAsync_Customer_WhenTableOccupiedWithActiveOrders_ThrowsConflictException()
    {
        // Arrange
        SetupDefaultLookupBehavior();
        SetupDefaultUoW();

        var occupiedTable = MakeTable(1, "T001", 4, TABLE_OCCUPIED_ID);

        _tableRepoMock
            .Setup(t => t.GetByCodeAsync("T001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(occupiedTable);

        _tableRepoMock
            .Setup(t => t.CountActiveOrdersAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var service = CreateService();
        var request = new CreateOrderRequestDTO
        {
            TableCode = "T001",
            Items = new List<CreateOrderItemDTO>
            {
                new() { DishId = 1, Quantity = 1, Price = 50000m }
            }
        };

        // Act & Assert
        await service.Invoking(s => s.CreateOrderAsync(request))
            .Should().ThrowAsync<ConflictException>()
            .WithMessage("*already occupied*");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "CreateOrderAsync_Customer")]
    public async Task CreateOrderAsync_Customer_WhenReservedTable_SetsToOccupied()
    {
        // Arrange
        SetupDefaultLookupBehavior();
        SetupDefaultUoW();
        SetupNoDefaultTax();

        var reservedTable = MakeTable(1, "T001", 4, TABLE_RESERVED_ID);

        _tableRepoMock
            .Setup(t => t.GetByCodeAsync("T001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservedTable);

        _customerServiceMock
            .Setup(c => c.GetGuestCustomerIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(68L);

        _orderRepoMock
            .Setup(r => r.CreateOrderAsync(It.IsAny<Order>(), It.IsAny<List<OrderItem>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(300L);

        var service = CreateService();
        var request = new CreateOrderRequestDTO
        {
            TableCode = "T001",
            Items = new List<CreateOrderItemDTO>
            {
                new() { DishId = 1, Quantity = 1, Price = 50000m }
            }
        };

        // Act
        var result = await service.CreateOrderAsync(request);

        // Assert
        reservedTable.TableStatusLvId.Should().Be(TABLE_OCCUPIED_ID);
        result.OrderId.Should().Be(300);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "CreateOrderAsync_Customer")]
    public async Task CreateOrderAsync_Customer_WhenNoQrToken_SkipsValidation()
    {
        // Arrange
        SetupDefaultLookupBehavior();
        SetupDefaultUoW();
        SetupNoDefaultTax();

        var table = MakeTable(1, "T001", 4, TABLE_AVAILABLE_ID);

        _tableRepoMock
            .Setup(t => t.GetByCodeAsync("T001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(table);

        _customerServiceMock
            .Setup(c => c.GetGuestCustomerIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(68L);

        _orderRepoMock
            .Setup(r => r.CreateOrderAsync(It.IsAny<Order>(), It.IsAny<List<OrderItem>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(400L);

        var service = CreateService();
        var request = new CreateOrderRequestDTO
        {
            TableCode = "T001",
            QrToken = null, // no QR token
            Items = new List<CreateOrderItemDTO>
            {
                new() { DishId = 1, Quantity = 1, Price = 50000m }
            }
        };

        // Act
        var result = await service.CreateOrderAsync(request);

        // Assert — should succeed without QR validation
        result.OrderId.Should().Be(400);
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════
    // TEST CASES — AddItemsToOrderAsync (customer)
    // ═══════════════════════════════════════════════════════════════════════

    #region AddItemsToOrderAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "AddItemsToOrderAsync")]
    public async Task AddItemsToOrderAsync_WhenValid_AddsItemsAndRecalcsTax()
    {
        // Arrange
        SetupDefaultLookupBehavior();
        SetupNoDefaultTax();

        var order = MakeOrder(statusId: ORDER_IN_PROGRESS_ID);

        _orderRepoMock
            .Setup(r => r.GetByIdForUpdateAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _dishRepoMock
            .Setup(d => d.GetByIdsAsync(It.IsAny<List<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Dish> { MakeDish(1) });

        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var service = CreateService();
        var request = new AddOrderItemsRequestDTO
        {
            Items = new List<CreateOrderItemDTO>
            {
                new() { DishId = 1, Quantity = 1, Price = 50000m }
            }
        };

        // Act
        await service.AddItemsToOrderAsync(1, request);

        // Assert
        _orderRepoMock.Verify(
            r => r.AddItemsToOrderAsync(1, It.IsAny<List<OrderItem>>(),
                It.IsAny<uint>(), It.IsAny<uint>(), It.IsAny<uint>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "AddItemsToOrderAsync")]
    public async Task AddItemsToOrderAsync_WhenOrderNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _orderRepoMock
            .Setup(r => r.GetByIdForUpdateAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var service = CreateService();
        var request = new AddOrderItemsRequestDTO
        {
            Items = new List<CreateOrderItemDTO>
            {
                new() { DishId = 1, Quantity = 1, Price = 50000m }
            }
        };

        // Act & Assert
        await service.Invoking(s => s.AddItemsToOrderAsync(999, request))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "AddItemsToOrderAsync")]
    public async Task AddItemsToOrderAsync_WhenOrderAlreadyPaid_ThrowsInvalidOperationException()
    {
        // Arrange
        var paidOrder = MakeOrder(statusId: ORDER_IN_PROGRESS_ID,
            payments: new List<Payment> { new() { PaymentId = 1, ReceivedAmount = 100m } });

        _orderRepoMock
            .Setup(r => r.GetByIdForUpdateAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paidOrder);

        var service = CreateService();
        var request = new AddOrderItemsRequestDTO
        {
            Items = new List<CreateOrderItemDTO>
            {
                new() { DishId = 1, Quantity = 1, Price = 50000m }
            }
        };

        // Act & Assert
        await service.Invoking(s => s.AddItemsToOrderAsync(1, request))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already been paid*");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "AddItemsToOrderAsync")]
    public async Task AddItemsToOrderAsync_WhenOrderIsPending_StillAddsItems()
    {
        // Arrange — PENDING status (boundary: typical usage is IN_PROGRESS)
        SetupDefaultLookupBehavior();
        SetupNoDefaultTax();

        var pendingOrder = MakeOrder(statusId: ORDER_PENDING_ID);

        _orderRepoMock
            .Setup(r => r.GetByIdForUpdateAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pendingOrder);

        _dishRepoMock
            .Setup(d => d.GetByIdsAsync(It.IsAny<List<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Dish> { MakeDish(1) });

        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var service = CreateService();
        var request = new AddOrderItemsRequestDTO
        {
            Items = new List<CreateOrderItemDTO>
            {
                new() { DishId = 1, Quantity = 1, Price = 50000m }
            }
        };

        // Act
        await service.AddItemsToOrderAsync(1, request);

        // Assert — items added successfully to PENDING order
        _orderRepoMock.Verify(
            r => r.AddItemsToOrderAsync(1, It.IsAny<List<OrderItem>>(),
                It.IsAny<uint>(), It.IsAny<uint>(), It.IsAny<uint>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════
    // TEST CASES — GetRecentOrdersAsync
    // ═══════════════════════════════════════════════════════════════════════

    #region GetRecentOrdersAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetRecentOrdersAsync")]
    public async Task GetRecentOrdersAsync_WhenCalled_ReturnsList()
    {
        // Arrange
        var orders = new List<RecentOrderDTO>
        {
            new() { OrderId = 1 },
            new() { OrderId = 2 }
        };

        _orderRepoMock
            .Setup(r => r.GetRecentOrdersAsync(1, It.IsAny<List<string>>(), 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        var service = CreateService();

        // Act
        var result = await service.GetRecentOrdersAsync(1, new List<string> { "ADMIN" }, 10, default);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetRecentOrdersAsync")]
    public async Task GetRecentOrdersAsync_WhenLimitIsZero_DefaultsTo20()
    {
        // Arrange
        _orderRepoMock
            .Setup(r => r.GetRecentOrdersAsync(1, It.IsAny<List<string>>(), 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecentOrderDTO>());

        var service = CreateService();

        // Act
        await service.GetRecentOrdersAsync(1, new List<string> { "ADMIN" }, 0, default);

        // Assert
        _orderRepoMock.Verify(
            r => r.GetRecentOrdersAsync(1, It.IsAny<List<string>>(), 20, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetRecentOrdersAsync")]
    public async Task GetRecentOrdersAsync_WhenLimitExceeds100_DefaultsTo20()
    {
        // Arrange
        _orderRepoMock
            .Setup(r => r.GetRecentOrdersAsync(1, It.IsAny<List<string>>(), 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecentOrderDTO>());

        var service = CreateService();

        // Act
        await service.GetRecentOrdersAsync(1, new List<string> { "ADMIN" }, 150, default);

        // Assert
        _orderRepoMock.Verify(
            r => r.GetRecentOrdersAsync(1, It.IsAny<List<string>>(), 20, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "GetRecentOrdersAsync")]
    public async Task GetRecentOrdersAsync_WhenLimitIsNegative_DefaultsTo20()
    {
        // Arrange — negative limit is abnormal input
        _orderRepoMock
            .Setup(r => r.GetRecentOrdersAsync(1, It.IsAny<List<string>>(), 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecentOrderDTO>());

        var service = CreateService();

        // Act
        await service.GetRecentOrdersAsync(1, new List<string> { "ADMIN" }, -5, default);

        // Assert — service clamps -5 → 20
        _orderRepoMock.Verify(
            r => r.GetRecentOrdersAsync(1, It.IsAny<List<string>>(), 20, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════
    // TEST CASES — UpdateOrderItemStatusAsync
    // ═══════════════════════════════════════════════════════════════════════

    #region UpdateOrderItemStatusAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "UpdateOrderItemStatusAsync")]
    public async Task UpdateOrderItemStatusAsync_WhenReady_DelegatesAndPublishesNotification()
    {
        // Arrange
        SetupDefaultLookupBehavior();

        _orderRepoMock
            .Setup(r => r.GetOrderItemAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OrderItem
            {
                OrderItemId = 1,
                DishId = 1,
                Dish = MakeDish(),
                Order = MakeOrder()
            });

        var service = CreateService();

        // Act
        await service.UpdateOrderItemStatusAsync(1, ITEM_READY_ID, null);

        // Assert
        _orderRepoMock.Verify(
            r => r.UpdateOrderItemStatusAsync(1, ITEM_READY_ID, null,
                It.IsAny<uint>(), It.IsAny<uint>(), It.IsAny<uint>(),
                It.IsAny<uint>(), It.IsAny<uint>(), It.IsAny<uint>(),
                It.IsAny<uint>(), It.IsAny<uint>(), It.IsAny<uint>(),
                It.IsAny<uint>(), It.IsAny<CancellationToken>()),
            Times.Once);

        _notificationServiceMock.Verify(
            n => n.PublishAsync(
                It.Is<PublishNotificationRequest>(r => r.Type == nameof(NotificationType.ORDER_ITEM_READY)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "UpdateOrderItemStatusAsync")]
    public async Task UpdateOrderItemStatusAsync_WhenRejected_PublishesRejectNotificationWithReason()
    {
        // Arrange
        SetupDefaultLookupBehavior();

        _orderRepoMock
            .Setup(r => r.GetOrderItemAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OrderItem
            {
                OrderItemId = 1,
                DishId = 1,
                Dish = MakeDish(),
                Order = MakeOrder()
            });

        var service = CreateService();

        // Act
        await service.UpdateOrderItemStatusAsync(1, ITEM_REJECTED_ID, "Out of stock");

        // Assert
        _notificationServiceMock.Verify(
            n => n.PublishAsync(
                It.Is<PublishNotificationRequest>(r => r.Type == nameof(NotificationType.ORDER_ITEM_REJECTED)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "UpdateOrderItemStatusAsync")]
    public async Task UpdateOrderItemStatusAsync_WhenStatusNotReadyOrRejected_NoNotification()
    {
        // Arrange
        SetupDefaultLookupBehavior();

        _orderRepoMock
            .Setup(r => r.GetOrderItemAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OrderItem
            {
                OrderItemId = 1,
                DishId = 1,
                Dish = MakeDish(),
                Order = MakeOrder()
            });

        var service = CreateService();

        // Act — SERVED status
        await service.UpdateOrderItemStatusAsync(1, ITEM_SERVED_ID, null);

        // Assert - no notification for SERVED
        _notificationServiceMock.Verify(
            n => n.PublishAsync(It.IsAny<PublishNotificationRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "UpdateOrderItemStatusAsync")]
    public async Task UpdateOrderItemStatusAsync_WhenInvalidItemId_PropagatesRepositoryException()
    {
        // Arrange — negative item ID is abnormal input
        SetupDefaultLookupBehavior();

        _orderRepoMock
            .Setup(r => r.UpdateOrderItemStatusAsync(
                -1, It.IsAny<uint>(), It.IsAny<string?>(),
                It.IsAny<uint>(), It.IsAny<uint>(), It.IsAny<uint>(),
                It.IsAny<uint>(), It.IsAny<uint>(), It.IsAny<uint>(),
                It.IsAny<uint>(), It.IsAny<uint>(), It.IsAny<uint>(),
                It.IsAny<uint>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("Order item not found"));

        var service = CreateService();

        // Act & Assert
        await service.Invoking(s => s.UpdateOrderItemStatusAsync(-1, ITEM_READY_ID, null))
            .Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*not found*");
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════
    // TEST CASES — CancelOrderItemAsync
    // ═══════════════════════════════════════════════════════════════════════

    #region CancelOrderItemAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "CancelOrderItemAsync")]
    public async Task CancelOrderItemAsync_WhenValid_CancelsItemAndNotifies()
    {
        // Arrange
        SetupDefaultLookupBehavior();

        var orderItem = new OrderItem
        {
            OrderItemId = 1,
            DishId = 1,
            Dish = new Dish { DishName = "Phở bò" },
            Order = new Order { Table = new RestaurantTable { TableCode = "T001" } }
        };

        _orderRepoMock
            .Setup(r => r.GetOrderItemAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(orderItem);

        var service = CreateService();

        // Act
        await service.CancelOrderItemAsync(1);

        // Assert — calls UpdateOrderItemStatusAsync with CANCELLED status, then fires notification
        _notificationServiceMock.Verify(
            n => n.PublishAsync(
                It.Is<PublishNotificationRequest>(r => r.Type == nameof(NotificationType.ORDER_ITEM_CANCELLED)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "CancelOrderItemAsync")]
    public async Task CancelOrderItemAsync_WhenOrderItemHasNoDish_StillCancels()
    {
        // Arrange
        SetupDefaultLookupBehavior();

        _orderRepoMock
            .Setup(r => r.GetOrderItemAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OrderItem
            {
                OrderItemId = 1,
                Dish = null!,
                Order = new Order { Table = null }
            });

        var service = CreateService();

        // Act
        await service.CancelOrderItemAsync(1);

        // Assert — metadata should handle null gracefully
        _notificationServiceMock.Verify(
            n => n.PublishAsync(It.IsAny<PublishNotificationRequest>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CancelOrderItemAsync")]
    public async Task CancelOrderItemAsync_WhenOrderItemNotFound_PropagatesException()
    {
        // Arrange — negative item ID is abnormal input
        _orderRepoMock
            .Setup(r => r.GetOrderItemAsync(-1, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("Order item not found"));

        var service = CreateService();

        // Act & Assert
        await service.Invoking(s => s.CancelOrderItemAsync(-1))
            .Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*not found*");
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════
    // TEST CASES — GetOrderByIdAsync
    // ═══════════════════════════════════════════════════════════════════════

    #region GetOrderByIdAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetOrderByIdAsync")]
    public async Task GetOrderByIdAsync_WhenCalled_DelegatesToRepository()
    {
        // Arrange
        var expected = new OrderDetailDTO { OrderId = 1 };

        _orderRepoMock
            .Setup(r => r.GetOrderByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var service = CreateService();

        // Act
        var result = await service.GetOrderByIdAsync(1);

        // Assert
        result.OrderId.Should().Be(1);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "GetOrderByIdAsync")]
    public async Task GetOrderByIdAsync_WhenIdIsNegative_ReturnsNull()
    {
        // Arrange — negative ID is abnormal input for a primary key
        _orderRepoMock
            .Setup(r => r.GetOrderByIdAsync(-1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(null as OrderDetailDTO);

        var service = CreateService();

        // Act
        var result = await service.GetOrderByIdAsync(-1);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetOrderByIdAsync")]
    public async Task GetOrderByIdAsync_WhenIdIsZero_DelegatesToRepository()
    {
        // Arrange — zero is boundary between valid and invalid IDs
        _orderRepoMock
            .Setup(r => r.GetOrderByIdAsync(0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(null as OrderDetailDTO);

        var service = CreateService();

        // Act
        var result = await service.GetOrderByIdAsync(0);

        // Assert
        result.Should().BeNull();
        _orderRepoMock.Verify(r => r.GetOrderByIdAsync(0, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
