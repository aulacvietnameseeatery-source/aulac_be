using Core.DTO.General;
using Core.DTO.Order;
using Core.DTO.Payment;
using Core.Entity;
using Core.Enum;
using LookupTypeEnum = Core.Enum.LookupType;
using Core.Interface.Repo;
using Core.Interface.Service.Entity;
using Core.Interface.Service.Notification;
using Core.Interface.Service.Shift;
using Core.Interface.Service.Others;
using Infa.Data;
using Infa.Service;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Tests.Services;

/// <summary>
/// Unit Test — PaymentService
/// Code Module : Infa/Service/PaymentService.cs
/// Method      : ProcessPaymentAsync, GetPaymentsAsync
/// Created By  : quantm
/// Executed By : quantm
/// Test Req.   : Verify payment processing logic including validating order state,
///               applying coupons and promotions, calculating final amount, creating
///               payment records, updating order/table status, awarding loyalty points,
///               and browsing payment history.
/// </summary>
public class PaymentServiceTests : IDisposable
{
    // ── Mocks ──
    private readonly Mock<ILookupResolver> _lookupResolverMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ISystemSettingService> _systemSettingMock = new();
    private readonly Mock<INotificationService> _notificationMock = new();
    private readonly Mock<IShiftLiveRealtimePublisher> _shiftPublisherMock = new();
    private readonly Mock<IPaymentRepository> _paymentRepoMock = new();
    private readonly Mock<IOrderRealtimeService> _orderRealtimeMock = new();

    // ── InMemory DbContext ──
    private readonly RestaurantMgmtContext _context;

    // ── Lookup ID Constants ──
    private const uint CancelledOrderStatusId = 10u;
    private const uint CompletedOrderStatusId = 11u;
    private const uint AvailableTableStatusId = 20u;
    private const uint OccupiedTableStatusId = 21u;
    private const uint RejectedItemStatusId = 30u;
    private const uint CancelledItemStatusId = 31u;
    private const uint ServedItemStatusId = 32u;
    private const uint ActiveCouponStatusId = 40u;
    private const uint ActivePromotionStatusId = 50u;
    private const uint CashMethodId = 60u;

    public PaymentServiceTests()
    {
        var options = new DbContextOptionsBuilder<RestaurantMgmtContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new RestaurantMgmtContext(options);

        SetupLookupResolver();
        SetupUnitOfWork();
        SetupSystemSettings(loyaltyEnabled: false);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    // ── Factory ──
    private PaymentService CreateService() => new(
        _context,
        _lookupResolverMock.Object,
        _unitOfWorkMock.Object,
        _systemSettingMock.Object,
        _notificationMock.Object,
        _shiftPublisherMock.Object,
        _paymentRepoMock.Object,
        _orderRealtimeMock.Object);

    // ── Setup Helpers ──
    private void SetupLookupResolver()
    {
        // OrderStatus
        _lookupResolverMock.Setup(r => r.GetIdAsync(
            (ushort)LookupTypeEnum.OrderStatus, OrderStatusCode.CANCELLED, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CancelledOrderStatusId);
        _lookupResolverMock.Setup(r => r.GetIdAsync(
            (ushort)LookupTypeEnum.OrderStatus, OrderStatusCode.COMPLETED, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CompletedOrderStatusId);

        // TableStatus
        _lookupResolverMock.Setup(r => r.GetIdAsync(
            (ushort)LookupTypeEnum.TableStatus, TableStatusCode.AVAILABLE, It.IsAny<CancellationToken>()))
            .ReturnsAsync(AvailableTableStatusId);

        // OrderItemStatus
        _lookupResolverMock.Setup(r => r.GetIdAsync(
            (ushort)LookupTypeEnum.OrderItemStatus, OrderItemStatusCode.REJECTED, It.IsAny<CancellationToken>()))
            .ReturnsAsync(RejectedItemStatusId);
        _lookupResolverMock.Setup(r => r.GetIdAsync(
            (ushort)LookupTypeEnum.OrderItemStatus, OrderItemStatusCode.CANCELLED, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CancelledItemStatusId);

        // CouponStatus
        _lookupResolverMock.Setup(r => r.GetIdAsync(
            (ushort)LookupTypeEnum.CouponStatus, CouponStatusCode.ACTIVE, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ActiveCouponStatusId);

        // PromotionStatus
        _lookupResolverMock.Setup(r => r.GetIdAsync(
            (ushort)LookupTypeEnum.PromotionStatus, PromotionStatusCode.ACTIVE, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ActivePromotionStatusId);

        // PaymentMethod (string-based lookup)
        _lookupResolverMock.Setup(r => r.GetIdAsync(
            (ushort)LookupTypeEnum.PaymentMethod, "CASH", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CashMethodId);
        _lookupResolverMock.Setup(r => r.GetIdAsync(
            (ushort)LookupTypeEnum.PaymentMethod, "CARD", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CashMethodId + 1);
    }

    private void SetupUnitOfWork()
    {
        _unitOfWorkMock.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
                       .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                       .Returns((CancellationToken ct) => _context.SaveChangesAsync(ct));
        _unitOfWorkMock.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
                       .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.RollbackAsync(It.IsAny<CancellationToken>()))
                       .Returns(Task.CompletedTask);
    }

    private void SetupSystemSettings(bool loyaltyEnabled, decimal pointBase = 10m)
    {
        _systemSettingMock.Setup(s => s.GetBoolAsync(
            "loyalty.enabled", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(loyaltyEnabled);

        if (loyaltyEnabled)
        {
            _systemSettingMock.Setup(s => s.GetDecimalAsync(
                "loyalty.point_base_amount", null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(pointBase);
        }
    }

    // ── Data Seeding ──
    private async Task<Order> SeedOrderAsync(
        long orderId = 1,
        long customerId = 1,
        long? tableId = null,
        uint? orderStatusLvId = null,
        decimal taxAmount = 0m,
        decimal? tipAmount = null,
        List<OrderItem>? items = null,
        bool hasPriorPayment = false)
    {
        var customer = new Customer
        {
            CustomerId = customerId,
            Phone = "0901234567",
            FullName = "Test Customer",
            IsMember = true,
            LoyaltyPoints = 0
        };

        // Only add if not exists
        if (!await _context.Customers.AnyAsync(c => c.CustomerId == customerId))
            _context.Customers.Add(customer);

        var order = new Order
        {
            OrderId = orderId,
            CustomerId = customerId,
            OrderStatusLvId = orderStatusLvId ?? 1u, // default active
            TotalAmount = 0m,
            SubTotalAmount = 0m,
            TaxAmount = taxAmount,
            TipAmount = tipAmount,
            SourceLvId = 1u,
            CreatedAt = DateTime.UtcNow
        };

        if (tableId.HasValue)
        {
            var table = new RestaurantTable
            {
                TableId = tableId.Value,
                TableCode = $"T{tableId.Value}",
                TableStatusLvId = OccupiedTableStatusId,
                TableTypeLvId = 1u,
                ZoneLvId = 1u,
                Capacity = 4,
                IsOnline = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            if (!await _context.RestaurantTables.AnyAsync(t => t.TableId == tableId.Value))
                _context.RestaurantTables.Add(table);

            order.TableId = tableId.Value;
        }

        _context.Orders.Add(order);

        if (items != null)
        {
            foreach (var item in items)
            {
                item.OrderId = orderId;
                _context.OrderItems.Add(item);
            }
        }
        else
        {
            // Default items — use orderId-based unique IDs to avoid PK collisions
            var dishId = orderId * 100;
            var textId = orderId * 100;
            _context.OrderItems.Add(new OrderItem
            {
                OrderId = orderId,
                DishId = dishId,
                Quantity = 2,
                Price = 100m,
                ItemStatusLvId = ServedItemStatusId,
                Dish = new Dish
                {
                    DishId = dishId,
                    DishName = "Pho",
                    CategoryId = 1,
                    DishStatusLvId = 1,
                    IsOnline = true,
                    DishNameTextId = textId,
                    DishNameText = new I18nText { TextId = textId, TextKey = $"dish_{textId}", SourceLangCode = "en", SourceText = "Pho" }
                }
            });
        }

        if (hasPriorPayment)
        {
            _context.Payments.Add(new Payment
            {
                OrderId = orderId,
                ReceivedAmount = 500m,
                ChangeAmount = 0m,
                MethodLvId = CashMethodId,
                PaidAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();
        return order;
    }

    // ══════════════════════════════════════════════════════════════════
    // ProcessPaymentAsync
    // ══════════════════════════════════════════════════════════════════

    #region ProcessPaymentAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "ProcessPaymentAsync")]
    public async Task ProcessPaymentAsync_WhenValidCashPayment_CompletesSuccessfully()
    {
        // Arrange
        await SeedOrderAsync(orderId: 1, customerId: 1);
        var dto = new CreatePaymentDTO
        {
            OrderId = 1,
            ReceivedAmount = 200m,
            PaymentMethod = "CASH"
        };
        var service = CreateService();

        // Act
        await service.ProcessPaymentAsync(dto);

        // Assert
        var order = await _context.Orders.FirstAsync(o => o.OrderId == 1);
        order.OrderStatusLvId.Should().Be(CompletedOrderStatusId);

        var payment = await _context.Payments.FirstOrDefaultAsync(p => p.OrderId == 1);
        payment.Should().NotBeNull();
        payment!.MethodLvId.Should().Be(CashMethodId);

        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _notificationMock.Verify(n => n.PublishAsync(It.IsAny<Core.DTO.Notification.PublishNotificationRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        _orderRealtimeMock.Verify(o => o.OrderPaidAsync(1), Times.Once);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "ProcessPaymentAsync")]
    public async Task ProcessPaymentAsync_WhenDineInOrder_SetsTableToAvailable()
    {
        // Arrange
        await SeedOrderAsync(orderId: 2, customerId: 2, tableId: 5);
        var dto = new CreatePaymentDTO
        {
            OrderId = 2,
            ReceivedAmount = 200m,
            PaymentMethod = "CASH"
        };
        var service = CreateService();

        // Act
        await service.ProcessPaymentAsync(dto);

        // Assert
        var table = await _context.RestaurantTables.FirstAsync(t => t.TableId == 5);
        table.TableStatusLvId.Should().Be(AvailableTableStatusId);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "ProcessPaymentAsync")]
    public async Task ProcessPaymentAsync_WhenTipProvided_IncludesTipInFinalAmount()
    {
        // Arrange
        await SeedOrderAsync(orderId: 3, customerId: 3);
        var dto = new CreatePaymentDTO
        {
            OrderId = 3,
            ReceivedAmount = 300m,
            PaymentMethod = "CASH",
            TipAmount = 50m
        };
        var service = CreateService();

        // Act
        await service.ProcessPaymentAsync(dto);

        // Assert
        var order = await _context.Orders.FirstAsync(o => o.OrderId == 3);
        order.TipAmount.Should().Be(50m);
        order.TotalAmount.Should().Be(250m); // 200 subtotal + 0 tax + 50 tip
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "ProcessPaymentAsync")]
    public async Task ProcessPaymentAsync_WhenLoyaltyEnabled_AwardsPoints()
    {
        // Arrange
        SetupSystemSettings(loyaltyEnabled: true, pointBase: 100m);
        await SeedOrderAsync(orderId: 4, customerId: 4);
        var dto = new CreatePaymentDTO
        {
            OrderId = 4,
            ReceivedAmount = 200m,
            PaymentMethod = "CASH"
        };
        var service = CreateService();

        // Act
        await service.ProcessPaymentAsync(dto);

        // Assert
        var customer = await _context.Customers.FirstAsync(c => c.CustomerId == 4);
        customer.LoyaltyPoints.Should().Be(2); // 200 / 100 = 2 points
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "ProcessPaymentAsync")]
    public async Task ProcessPaymentAsync_WhenGuestCustomer_DoesNotAwardLoyaltyPoints()
    {
        // Arrange
        SetupSystemSettings(loyaltyEnabled: true, pointBase: 100m);
        // GuestCustomerId = 68 (hardcoded constant in PaymentService)
        await SeedOrderAsync(orderId: 5, customerId: 68);
        var dto = new CreatePaymentDTO
        {
            OrderId = 5,
            ReceivedAmount = 200m,
            PaymentMethod = "CASH"
        };
        var service = CreateService();

        // Act
        await service.ProcessPaymentAsync(dto);

        // Assert
        var customer = await _context.Customers.FirstAsync(c => c.CustomerId == 68);
        customer.LoyaltyPoints.Should().Be(0);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "ProcessPaymentAsync")]
    public async Task ProcessPaymentAsync_WhenReceivedMoreThanFinal_CalculatesChange()
    {
        // Arrange
        await SeedOrderAsync(orderId: 6, customerId: 6);
        var dto = new CreatePaymentDTO
        {
            OrderId = 6,
            ReceivedAmount = 500m, // order subtotal is 200
            PaymentMethod = "CASH"
        };
        var service = CreateService();

        // Act
        await service.ProcessPaymentAsync(dto);

        // Assert
        var payment = await _context.Payments.FirstAsync(p => p.OrderId == 6);
        payment.ChangeAmount.Should().Be(300m); // 500 - 200
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "ProcessPaymentAsync")]
    public async Task ProcessPaymentAsync_WhenOrderNotFound_ThrowsKeyNotFound()
    {
        // Arrange
        var dto = new CreatePaymentDTO
        {
            OrderId = 999,
            ReceivedAmount = 100m,
            PaymentMethod = "CASH"
        };
        var service = CreateService();

        // Act
        Func<Task> act = () => service.ProcessPaymentAsync(dto);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
                 .WithMessage("*Order*not found*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "ProcessPaymentAsync")]
    public async Task ProcessPaymentAsync_WhenOrderIsCancelled_ThrowsInvalidOperation()
    {
        // Arrange
        await SeedOrderAsync(orderId: 7, customerId: 7, orderStatusLvId: CancelledOrderStatusId);
        var dto = new CreatePaymentDTO
        {
            OrderId = 7,
            ReceivedAmount = 200m,
            PaymentMethod = "CASH"
        };
        var service = CreateService();

        // Act
        Func<Task> act = () => service.ProcessPaymentAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("*Cannot pay for a cancelled order*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "ProcessPaymentAsync")]
    public async Task ProcessPaymentAsync_WhenAlreadyPaid_ThrowsInvalidOperation()
    {
        // Arrange
        await SeedOrderAsync(orderId: 8, customerId: 8, hasPriorPayment: true);
        var dto = new CreatePaymentDTO
        {
            OrderId = 8,
            ReceivedAmount = 200m,
            PaymentMethod = "CASH"
        };
        var service = CreateService();

        // Act
        Func<Task> act = () => service.ProcessPaymentAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("*already been paid*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "ProcessPaymentAsync")]
    public async Task ProcessPaymentAsync_WhenReceivedLessThanFinal_ThrowsInvalidOperation()
    {
        // Arrange
        await SeedOrderAsync(orderId: 9, customerId: 9);
        var dto = new CreatePaymentDTO
        {
            OrderId = 9,
            ReceivedAmount = 1m, // subtotal is 200
            PaymentMethod = "CASH"
        };
        var service = CreateService();

        // Act
        Func<Task> act = () => service.ProcessPaymentAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("*Received amount cannot be less than final amount*");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "ProcessPaymentAsync")]
    public async Task ProcessPaymentAsync_WhenRejectedItemsExist_ExcludesFromSubtotal()
    {
        // Arrange
        var items = new List<OrderItem>
        {
            new()
            {
                DishId = 10,
                Quantity = 2,
                Price = 100m,
                ItemStatusLvId = ServedItemStatusId,
                Dish = new Dish { DishId = 100, DishName = "A", CategoryId = 1, DishStatusLvId = 1, IsOnline = true, DishNameTextId = 100, DishNameText = new I18nText { TextId = 100, TextKey = "dish_100", SourceLangCode = "en", SourceText = "A" } }
            },
            new()
            {
                DishId = 20,
                Quantity = 1,
                Price = 500m,
                ItemStatusLvId = RejectedItemStatusId, // this should be excluded
                Dish = new Dish { DishId = 200, DishName = "B", CategoryId = 1, DishStatusLvId = 1, IsOnline = true, DishNameTextId = 200, DishNameText = new I18nText { TextId = 200, TextKey = "dish_200", SourceLangCode = "en", SourceText = "B" } }
            }
        };
        await SeedOrderAsync(orderId: 10, customerId: 10, items: items);
        var dto = new CreatePaymentDTO
        {
            OrderId = 10,
            ReceivedAmount = 200m, // only non-rejected: 2 * 100 = 200
            PaymentMethod = "CASH"
        };
        var service = CreateService();

        // Act
        await service.ProcessPaymentAsync(dto);

        // Assert
        var order = await _context.Orders.FirstAsync(o => o.OrderId == 10);
        order.SubTotalAmount.Should().Be(200m); // only the served item
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "ProcessPaymentAsync")]
    public async Task ProcessPaymentAsync_WhenReceivedExactlyEqualsFinal_ZeroChange()
    {
        // Arrange
        await SeedOrderAsync(orderId: 11, customerId: 11);
        var dto = new CreatePaymentDTO
        {
            OrderId = 11,
            ReceivedAmount = 200m, // exactly equal to subtotal
            PaymentMethod = "CASH"
        };
        var service = CreateService();

        // Act
        await service.ProcessPaymentAsync(dto);

        // Assert
        var payment = await _context.Payments.FirstAsync(p => p.OrderId == 11);
        payment.ChangeAmount.Should().Be(0m);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "ProcessPaymentAsync")]
    public async Task ProcessPaymentAsync_WhenCardPayment_UsesCardMethodId()
    {
        // Arrange
        await SeedOrderAsync(orderId: 12, customerId: 12);
        var dto = new CreatePaymentDTO
        {
            OrderId = 12,
            ReceivedAmount = 200m,
            PaymentMethod = "CARD"
        };
        var service = CreateService();

        // Act
        await service.ProcessPaymentAsync(dto);

        // Assert — Verify CARD method ID is stored (CashMethodId + 1 per setup)
        var payment = await _context.Payments.FirstAsync(p => p.OrderId == 12);
        payment.MethodLvId.Should().Be(CashMethodId + 1);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "ProcessPaymentAsync")]
    public async Task ProcessPaymentAsync_WhenTaxAmountIncluded_AddsTaxToFinal()
    {
        // Arrange — order with tax = 20
        await SeedOrderAsync(orderId: 13, customerId: 13, taxAmount: 20m);
        var dto = new CreatePaymentDTO
        {
            OrderId = 13,
            ReceivedAmount = 220m, // subtotal 200 + tax 20
            PaymentMethod = "CASH"
        };
        var service = CreateService();

        // Act
        await service.ProcessPaymentAsync(dto);

        // Assert — Verify final includes tax
        var order = await _context.Orders.FirstAsync(o => o.OrderId == 13);
        order.TotalAmount.Should().Be(220m); // 200 subtotal + 20 tax
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "ProcessPaymentAsync")]
    public async Task ProcessPaymentAsync_WhenTipAmountIsZero_FinalEqualsSubtotalPlusTax()
    {
        // Arrange — explicit TipAmount = 0
        await SeedOrderAsync(orderId: 14, customerId: 14);
        var dto = new CreatePaymentDTO
        {
            OrderId = 14,
            ReceivedAmount = 200m,
            PaymentMethod = "CASH",
            TipAmount = 0m
        };
        var service = CreateService();

        // Act
        await service.ProcessPaymentAsync(dto);

        // Assert — Verify zero tip does not affect final amount
        var order = await _context.Orders.FirstAsync(o => o.OrderId == 14);
        order.TotalAmount.Should().Be(200m); // subtotal only, no tip, no tax
        order.TipAmount.Should().Be(0m);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "ProcessPaymentAsync")]
    public async Task ProcessPaymentAsync_WhenLoyaltyDisabled_DoesNotAwardPoints()
    {
        // Arrange — loyalty explicitly disabled (default in constructor)
        await SeedOrderAsync(orderId: 15, customerId: 15);
        var dto = new CreatePaymentDTO
        {
            OrderId = 15,
            ReceivedAmount = 200m,
            PaymentMethod = "CASH"
        };
        var service = CreateService();

        // Act
        await service.ProcessPaymentAsync(dto);

        // Assert — Verify no loyalty points when disabled
        var customer = await _context.Customers.FirstAsync(c => c.CustomerId == 15);
        customer.LoyaltyPoints.Should().Be(0);
    }

    #endregion

    // ══════════════════════════════════════════════════════════════════
    // GetPaymentsAsync
    // ══════════════════════════════════════════════════════════════════

    #region GetPaymentsAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetPaymentsAsync")]
    public async Task GetPaymentsAsync_WhenCalled_DelegatesToRepository()
    {
        // Arrange
        var query = new PaymentListQueryDTO { PageIndex = 1, PageSize = 10 };
        var expected = new PagedResultDTO<PaymentListDTO>
        {
            PageData = new List<PaymentListDTO>
            {
                new() { PaymentId = 1, OrderId = 1, ReceivedAmount = 200m }
            },
            PageIndex = 1,
            PageSize = 10,
            TotalCount = 1
        };
        _paymentRepoMock.Setup(r => r.GetPaymentsAsync(query, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(expected);
        var service = CreateService();

        // Act
        var result = await service.GetPaymentsAsync(query, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(expected);
        _paymentRepoMock.Verify(r => r.GetPaymentsAsync(query, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetPaymentsAsync")]
    public async Task GetPaymentsAsync_WhenNoPayments_ReturnsEmptyPage()
    {
        // Arrange
        var query = new PaymentListQueryDTO { PageIndex = 1, PageSize = 10 };
        var expected = new PagedResultDTO<PaymentListDTO>
        {
            PageData = new List<PaymentListDTO>(),
            PageIndex = 1,
            PageSize = 10,
            TotalCount = 0
        };
        _paymentRepoMock.Setup(r => r.GetPaymentsAsync(query, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(expected);
        var service = CreateService();

        // Act
        var result = await service.GetPaymentsAsync(query, CancellationToken.None);

        // Assert
        result.PageData.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    #endregion
}
