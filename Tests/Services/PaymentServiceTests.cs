using Core.DTO.General;
using Core.DTO.Notification;
using Core.DTO.Order;
using Core.DTO.Payment;
using Core.DTO.Shift;
using Core.Entity;
using Core.Enum;
using LookupTypeEnum = Core.Enum.LookupType;
using Core.Interface.Repo;
using Core.Interface.Service.Entity;
using Core.Interface.Service.Notification;
using Core.Interface.Service.Shift;
using Core.Interface.Service.Others;
using Infa.Service;
using FluentAssertions;
using Moq;

namespace Tests.Services;

/// <summary>
/// Unit Test — PaymentService
/// Code Module : Core/Service/PaymentService.cs
/// Method      : ProcessPaymentAsync, GetPaymentsAsync
/// Created By  : quantm
/// Executed By : quantm
/// Test Req.   : Verify payment processing logic including validating order state,
///               applying coupons and promotions, calculating final amount, creating
///               payment records, updating order/table status, awarding loyalty points,
///               and browsing payment history.
/// </summary>
public class PaymentServiceTests
{
    // ── Mocks ──
    private readonly Mock<ILookupResolver> _lookupResolverMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ISystemSettingService> _systemSettingMock = new();
    private readonly Mock<INotificationService> _notificationMock = new();
    private readonly Mock<IShiftLiveRealtimePublisher> _shiftPublisherMock = new();
    private readonly Mock<IPaymentRepository> _paymentRepoMock = new();
    private readonly Mock<IOrderRealtimeService> _orderRealtimeMock = new();

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
    private const uint CardMethodId = 61u;

    public PaymentServiceTests()
    {
        SetupLookupResolver();
        SetupUnitOfWork();
        SetupSystemSettings(loyaltyEnabled: false);
    }

    // ── Factory ──
    private PaymentService CreateService() => new(
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
            .ReturnsAsync(CardMethodId);
    }

    private void SetupUnitOfWork()
    {
        _unitOfWorkMock.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
                       .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                       .Returns(Task.CompletedTask);
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

    // ── Data Builders ──
    private static Order MakeValidOrder(
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

        var order = new Order
        {
            OrderId = orderId,
            CustomerId = customerId,
            Customer = customer,
            OrderStatusLvId = orderStatusLvId ?? 1u,
            TotalAmount = 0m,
            SubTotalAmount = 0m,
            TaxAmount = taxAmount,
            TipAmount = tipAmount,
            SourceLvId = 1u,
            TableId = tableId,
            CreatedAt = DateTime.UtcNow,
            OrderItems = items ?? new List<OrderItem>
            {
                new()
                {
                    OrderItemId = orderId * 100,
                    OrderId = orderId,
                    DishId = orderId * 100,
                    Quantity = 2,
                    Price = 100m,
                    ItemStatusLvId = ServedItemStatusId,
                    Dish = new Dish
                    {
                        DishId = orderId * 100,
                        DishName = "Pho",
                        CategoryId = 1,
                        DishStatusLvId = 1,
                        IsOnline = true,
                        DishNameTextId = orderId * 100,
                        DishNameText = new I18nText
                        {
                            TextId = orderId * 100,
                            TextKey = $"dish_{orderId * 100}",
                            SourceLangCode = "en",
                            SourceText = "Pho"
                        }
                    }
                }
            },
            Payments = new List<Payment>(),
            OrderCoupons = new List<OrderCoupon>(),
            OrderPromotions = new List<OrderPromotion>()
        };

        if (hasPriorPayment)
        {
            order.Payments.Add(new Payment
            {
                OrderId = orderId,
                ReceivedAmount = 500m,
                ChangeAmount = 0m,
                MethodLvId = CashMethodId,
                PaidAt = DateTime.UtcNow
            });
        }

        return order;
    }

    private static RestaurantTable MakeTable(long tableId)
    {
        return new RestaurantTable
        {
            TableId = tableId,
            TableCode = $"T{tableId}",
            TableStatusLvId = OccupiedTableStatusId,
            TableTypeLvId = 1u,
            ZoneLvId = 1u,
            Capacity = 4,
            IsOnline = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private static Coupon MakeActiveCoupon(
        long couponId,
        string code,
        string discountType,
        decimal discountValue,
        long? customerId = null,
        int? maxUsage = null,
        int usedCount = 0)
    {
        return new Coupon
        {
            CouponId = couponId,
            CouponCode = code,
            CouponName = $"Coupon {code}",
            CouponStatusLvId = ActiveCouponStatusId,
            DiscountValue = discountValue,
            StartTime = DateTime.UtcNow.AddDays(-1),
            EndTime = DateTime.UtcNow.AddDays(1),
            MaxUsage = maxUsage,
            UsedCount = usedCount,
            CustomerId = customerId,
            TypeLv = new LookupValue { ValueId = 1, ValueCode = discountType, ValueName = discountType, TypeId = 1, SortOrder = 1 }
        };
    }

    private void SetupOrderRepo(Order order)
    {
        _paymentRepoMock.Setup(r => r.GetOrderForPaymentAsync(order.OrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
    }

    private void SetupNoActivePromotions()
    {
        _paymentRepoMock.Setup(r => r.GetActivePromotionsAsync(
            It.IsAny<uint>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Promotion>());
    }

    private void SetupAddPayment()
    {
        _paymentRepoMock.Setup(r => r.AddPaymentAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private void SetupTableRepo(RestaurantTable table)
    {
        _paymentRepoMock.Setup(r => r.GetTableByIdAsync(table.TableId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(table);
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
        var order = MakeValidOrder(orderId: 1);
        SetupOrderRepo(order);
        SetupNoActivePromotions();
        SetupAddPayment();

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
        order.OrderStatusLvId.Should().Be(CompletedOrderStatusId);
        _paymentRepoMock.Verify(r => r.AddPaymentAsync(
            It.Is<Payment>(p => p.OrderId == 1 && p.MethodLvId == CashMethodId),
            It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _notificationMock.Verify(n => n.PublishAsync(
            It.IsAny<PublishNotificationRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        _orderRealtimeMock.Verify(o => o.OrderPaidAsync(1), Times.Once);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "ProcessPaymentAsync")]
    public async Task ProcessPaymentAsync_WhenDineInOrder_SetsTableToAvailable()
    {
        // Arrange
        var table = MakeTable(tableId: 5);
        var order = MakeValidOrder(orderId: 2, customerId: 2, tableId: 5);
        SetupOrderRepo(order);
        SetupTableRepo(table);
        SetupNoActivePromotions();
        SetupAddPayment();

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
        table.TableStatusLvId.Should().Be(AvailableTableStatusId);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "ProcessPaymentAsync")]
    public async Task ProcessPaymentAsync_WhenTipProvided_IncludesTipInFinalAmount()
    {
        // Arrange
        var order = MakeValidOrder(orderId: 3, customerId: 3);
        SetupOrderRepo(order);
        SetupNoActivePromotions();
        SetupAddPayment();

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
        var order = MakeValidOrder(orderId: 4, customerId: 4);
        SetupOrderRepo(order);
        SetupNoActivePromotions();
        SetupAddPayment();

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
        order.Customer.LoyaltyPoints.Should().Be(2); // 200 / 100 = 2 points
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "ProcessPaymentAsync")]
    public async Task ProcessPaymentAsync_WhenReceivedMoreThanFinal_CalculatesChange()
    {
        // Arrange
        var order = MakeValidOrder(orderId: 6, customerId: 6);
        SetupOrderRepo(order);
        SetupNoActivePromotions();
        SetupAddPayment();

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
        _paymentRepoMock.Verify(r => r.AddPaymentAsync(
            It.Is<Payment>(p => p.ChangeAmount == 300m), // 500 - 200
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "ProcessPaymentAsync")]
    public async Task ProcessPaymentAsync_WhenCardPayment_UsesCardMethodId()
    {
        // Arrange
        var order = MakeValidOrder(orderId: 12, customerId: 12);
        SetupOrderRepo(order);
        SetupNoActivePromotions();
        SetupAddPayment();

        var dto = new CreatePaymentDTO
        {
            OrderId = 12,
            ReceivedAmount = 200m,
            PaymentMethod = "CARD"
        };
        var service = CreateService();

        // Act
        await service.ProcessPaymentAsync(dto);

        // Assert
        _paymentRepoMock.Verify(r => r.AddPaymentAsync(
            It.Is<Payment>(p => p.MethodLvId == CardMethodId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "ProcessPaymentAsync")]
    public async Task ProcessPaymentAsync_WhenCouponApplied_ReducesFinalAmount()
    {
        // Arrange
        var order = MakeValidOrder(orderId: 16, customerId: 16);
        SetupOrderRepo(order);
        SetupNoActivePromotions();
        SetupAddPayment();

        var coupon = MakeActiveCoupon(
            couponId: 1,
            code: "SAVE50",
            discountType: "FIXED_AMOUNT",
            discountValue: 50m,
            customerId: 16);
        _paymentRepoMock.Setup(r => r.GetCouponWithTypeAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(coupon);

        var dto = new CreatePaymentDTO
        {
            OrderId = 16,
            ReceivedAmount = 150m, // 200 subtotal - 50 coupon = 150
            PaymentMethod = "CASH",
            CouponIds = new List<long> { 1 }
        };
        var service = CreateService();

        // Act
        await service.ProcessPaymentAsync(dto);

        // Assert
        order.TotalAmount.Should().Be(150m);
        order.OrderCoupons.Should().HaveCount(1);
        order.OrderCoupons.First().DiscountAmount.Should().Be(50m);
        coupon.UsedCount.Should().Be(1);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "ProcessPaymentAsync")]
    public async Task ProcessPaymentAsync_WhenPercentCouponApplied_CalculatesDiscount()
    {
        // Arrange
        var order = MakeValidOrder(orderId: 17, customerId: 17);
        SetupOrderRepo(order);
        SetupNoActivePromotions();
        SetupAddPayment();

        var coupon = MakeActiveCoupon(
            couponId: 2,
            code: "SAVE10PCT",
            discountType: "PERCENT",
            discountValue: 10m,
            customerId: 17);
        _paymentRepoMock.Setup(r => r.GetCouponWithTypeAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(coupon);

        var dto = new CreatePaymentDTO
        {
            OrderId = 17,
            ReceivedAmount = 180m, // 200 - 10% = 180
            PaymentMethod = "CASH",
            CouponIds = new List<long> { 2 }
        };
        var service = CreateService();

        // Act
        await service.ProcessPaymentAsync(dto);

        // Assert
        order.TotalAmount.Should().Be(180m);
        order.OrderCoupons.First().DiscountAmount.Should().Be(20m); // 10% of 200
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "ProcessPaymentAsync")]
    public async Task ProcessPaymentAsync_WhenGeneralPromotionActive_AppliesDiscount()
    {
        // Arrange
        var order = MakeValidOrder(orderId: 18, customerId: 18);
        SetupOrderRepo(order);
        SetupAddPayment();

        var promotion = new Promotion
        {
            PromotionId = 1,
            PromoName = "Holiday Sale",
            DiscountValue = 30m,
            PromotionStatusLvId = ActivePromotionStatusId,
            StartTime = DateTime.UtcNow.AddDays(-1),
            EndTime = DateTime.UtcNow.AddDays(1),
            UsedCount = 0,
            TypeLv = new LookupValue { ValueId = 1, ValueCode = "FIXED_AMOUNT", ValueName = "Fixed Amount", TypeId = 1, SortOrder = 1 },
            PromotionRules = new List<PromotionRule>(),
            PromotionTargets = new List<PromotionTarget>() // general promo — no targets
        };
        _paymentRepoMock.Setup(r => r.GetActivePromotionsAsync(
            It.IsAny<uint>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Promotion> { promotion });

        var dto = new CreatePaymentDTO
        {
            OrderId = 18,
            ReceivedAmount = 170m, // 200 - 30 = 170
            PaymentMethod = "CASH"
        };
        var service = CreateService();

        // Act
        await service.ProcessPaymentAsync(dto);

        // Assert
        order.TotalAmount.Should().Be(170m);
        order.OrderPromotions.Should().HaveCount(1);
        order.OrderPromotions.First().DiscountAmount.Should().Be(30m);
        promotion.UsedCount.Should().Be(1);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "ProcessPaymentAsync")]
    public async Task ProcessPaymentAsync_WhenDishTargetedPromotion_AppliesDiscountToMatchingItem()
    {
        // Arrange
        var items = new List<OrderItem>
        {
            new()
            {
                OrderItemId = 1901,
                OrderId = 19,
                DishId = 50,
                Quantity = 2,
                Price = 100m,
                ItemStatusLvId = ServedItemStatusId,
                Dish = new Dish { DishId = 50, DishName = "Pho", CategoryId = 1, DishStatusLvId = 1, IsOnline = true,
                    DishNameTextId = 1901,
                    DishNameText = new I18nText { TextId = 1901, TextKey = "dish_1901", SourceLangCode = "en", SourceText = "Pho" } }
            }
        };
        var order = MakeValidOrder(orderId: 19, customerId: 19, items: items);
        SetupOrderRepo(order);
        SetupAddPayment();

        var promotion = new Promotion
        {
            PromotionId = 2,
            PromoName = "Pho Discount",
            DiscountValue = 10m, // 10% off matching dishes
            PromotionStatusLvId = ActivePromotionStatusId,
            StartTime = DateTime.UtcNow.AddDays(-1),
            EndTime = DateTime.UtcNow.AddDays(1),
            UsedCount = 0,
            TypeLv = new LookupValue { ValueId = 1, ValueCode = "PERCENT", ValueName = "Percent", TypeId = 1, SortOrder = 1 },
            PromotionRules = new List<PromotionRule>(),
            PromotionTargets = new List<PromotionTarget>
            {
                new() { TargetId = 1, PromotionId = 2, DishId = 50 }
            }
        };
        _paymentRepoMock.Setup(r => r.GetActivePromotionsAsync(
            It.IsAny<uint>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Promotion> { promotion });

        var dto = new CreatePaymentDTO
        {
            OrderId = 19,
            ReceivedAmount = 180m, // 200 - 10% of 200 = 180
            PaymentMethod = "CASH"
        };
        var service = CreateService();

        // Act
        await service.ProcessPaymentAsync(dto);

        // Assert
        order.TotalAmount.Should().Be(180m);
        order.OrderPromotions.Should().HaveCount(1);
        order.OrderPromotions.First().DiscountAmount.Should().Be(20m); // 10% of (2 * 100)
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "ProcessPaymentAsync")]
    public async Task ProcessPaymentAsync_WhenGuestCustomer_DoesNotAwardLoyaltyPoints()
    {
        // Arrange
        SetupSystemSettings(loyaltyEnabled: true, pointBase: 100m);
        // GuestCustomerId = 68
        var order = MakeValidOrder(orderId: 5, customerId: 68);
        SetupOrderRepo(order);
        SetupNoActivePromotions();
        SetupAddPayment();

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
        order.Customer.LoyaltyPoints.Should().Be(0);
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
                OrderItemId = 1001,
                OrderId = 10,
                DishId = 10,
                Quantity = 2,
                Price = 100m,
                ItemStatusLvId = ServedItemStatusId,
                Dish = new Dish { DishId = 100, DishName = "A", CategoryId = 1, DishStatusLvId = 1, IsOnline = true,
                    DishNameTextId = 100,
                    DishNameText = new I18nText { TextId = 100, TextKey = "dish_100", SourceLangCode = "en", SourceText = "A" } }
            },
            new()
            {
                OrderItemId = 1002,
                OrderId = 10,
                DishId = 20,
                Quantity = 1,
                Price = 500m,
                ItemStatusLvId = RejectedItemStatusId, // excluded
                Dish = new Dish { DishId = 200, DishName = "B", CategoryId = 1, DishStatusLvId = 1, IsOnline = true,
                    DishNameTextId = 200,
                    DishNameText = new I18nText { TextId = 200, TextKey = "dish_200", SourceLangCode = "en", SourceText = "B" } }
            }
        };
        var order = MakeValidOrder(orderId: 10, customerId: 10, items: items);
        SetupOrderRepo(order);
        SetupNoActivePromotions();
        SetupAddPayment();

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
        order.SubTotalAmount.Should().Be(200m);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "ProcessPaymentAsync")]
    public async Task ProcessPaymentAsync_WhenCancelledItemsExist_ExcludesFromSubtotal()
    {
        // Arrange
        var items = new List<OrderItem>
        {
            new()
            {
                OrderItemId = 2001,
                OrderId = 20,
                DishId = 30,
                Quantity = 1,
                Price = 150m,
                ItemStatusLvId = ServedItemStatusId,
                Dish = new Dish { DishId = 300, DishName = "C", CategoryId = 1, DishStatusLvId = 1, IsOnline = true,
                    DishNameTextId = 300,
                    DishNameText = new I18nText { TextId = 300, TextKey = "dish_300", SourceLangCode = "en", SourceText = "C" } }
            },
            new()
            {
                OrderItemId = 2002,
                OrderId = 20,
                DishId = 40,
                Quantity = 3,
                Price = 200m,
                ItemStatusLvId = CancelledItemStatusId, // excluded
                Dish = new Dish { DishId = 400, DishName = "D", CategoryId = 1, DishStatusLvId = 1, IsOnline = true,
                    DishNameTextId = 400,
                    DishNameText = new I18nText { TextId = 400, TextKey = "dish_400", SourceLangCode = "en", SourceText = "D" } }
            }
        };
        var order = MakeValidOrder(orderId: 20, customerId: 20, items: items);
        SetupOrderRepo(order);
        SetupNoActivePromotions();
        SetupAddPayment();

        var dto = new CreatePaymentDTO
        {
            OrderId = 20,
            ReceivedAmount = 150m,
            PaymentMethod = "CASH"
        };
        var service = CreateService();

        // Act
        await service.ProcessPaymentAsync(dto);

        // Assert
        order.SubTotalAmount.Should().Be(150m);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "ProcessPaymentAsync")]
    public async Task ProcessPaymentAsync_WhenReceivedExactlyEqualsFinal_ZeroChange()
    {
        // Arrange
        var order = MakeValidOrder(orderId: 11, customerId: 11);
        SetupOrderRepo(order);
        SetupNoActivePromotions();
        SetupAddPayment();

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
        _paymentRepoMock.Verify(r => r.AddPaymentAsync(
            It.Is<Payment>(p => p.ChangeAmount == 0m),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "ProcessPaymentAsync")]
    public async Task ProcessPaymentAsync_WhenTaxAmountIncluded_AddsTaxToFinal()
    {
        // Arrange
        var order = MakeValidOrder(orderId: 13, customerId: 13, taxAmount: 20m);
        SetupOrderRepo(order);
        SetupNoActivePromotions();
        SetupAddPayment();

        var dto = new CreatePaymentDTO
        {
            OrderId = 13,
            ReceivedAmount = 220m, // subtotal 200 + tax 20
            PaymentMethod = "CASH"
        };
        var service = CreateService();

        // Act
        await service.ProcessPaymentAsync(dto);

        // Assert
        order.TotalAmount.Should().Be(220m); // 200 subtotal + 20 tax
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "ProcessPaymentAsync")]
    public async Task ProcessPaymentAsync_WhenTipAmountIsZero_FinalEqualsSubtotalPlusTax()
    {
        // Arrange
        var order = MakeValidOrder(orderId: 14, customerId: 14);
        SetupOrderRepo(order);
        SetupNoActivePromotions();
        SetupAddPayment();

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

        // Assert
        order.TotalAmount.Should().Be(200m);
        order.TipAmount.Should().Be(0m);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "ProcessPaymentAsync")]
    public async Task ProcessPaymentAsync_WhenLoyaltyDisabled_DoesNotAwardPoints()
    {
        // Arrange — loyalty explicitly disabled (default in constructor)
        var order = MakeValidOrder(orderId: 15, customerId: 15);
        SetupOrderRepo(order);
        SetupNoActivePromotions();
        SetupAddPayment();

        var dto = new CreatePaymentDTO
        {
            OrderId = 15,
            ReceivedAmount = 200m,
            PaymentMethod = "CASH"
        };
        var service = CreateService();

        // Act
        await service.ProcessPaymentAsync(dto);

        // Assert
        order.Customer.LoyaltyPoints.Should().Be(0);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "ProcessPaymentAsync")]
    public async Task ProcessPaymentAsync_WhenCouponNotBelongToCustomer_ThrowsInvalidOperation()
    {
        // Arrange
        var order = MakeValidOrder(orderId: 21, customerId: 21);
        SetupOrderRepo(order);
        SetupNoActivePromotions();
        SetupAddPayment();

        var coupon = MakeActiveCoupon(couponId: 5, code: "OTHER", discountType: "FIXED_AMOUNT",
            discountValue: 20m, customerId: 999); // belongs to different customer
        _paymentRepoMock.Setup(r => r.GetCouponWithTypeAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(coupon);

        var dto = new CreatePaymentDTO
        {
            OrderId = 21,
            ReceivedAmount = 200m,
            PaymentMethod = "CASH",
            CouponIds = new List<long> { 5 }
        };
        var service = CreateService();

        // Act
        Func<Task> act = () => service.ProcessPaymentAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("*does not belong to this customer*");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "ProcessPaymentAsync")]
    public async Task ProcessPaymentAsync_WhenCouponNotActive_ThrowsInvalidOperation()
    {
        // Arrange
        var order = MakeValidOrder(orderId: 22, customerId: 22);
        SetupOrderRepo(order);
        SetupNoActivePromotions();
        SetupAddPayment();

        var coupon = MakeActiveCoupon(couponId: 6, code: "DISABLED", discountType: "FIXED_AMOUNT",
            discountValue: 20m, customerId: 22);
        coupon.CouponStatusLvId = 999u; // not active
        _paymentRepoMock.Setup(r => r.GetCouponWithTypeAsync(6, It.IsAny<CancellationToken>()))
            .ReturnsAsync(coupon);

        var dto = new CreatePaymentDTO
        {
            OrderId = 22,
            ReceivedAmount = 200m,
            PaymentMethod = "CASH",
            CouponIds = new List<long> { 6 }
        };
        var service = CreateService();

        // Act
        Func<Task> act = () => service.ProcessPaymentAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("*is not active*");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "ProcessPaymentAsync")]
    public async Task ProcessPaymentAsync_WhenCouponExpired_ThrowsInvalidOperation()
    {
        // Arrange
        var order = MakeValidOrder(orderId: 23, customerId: 23);
        SetupOrderRepo(order);
        SetupNoActivePromotions();
        SetupAddPayment();

        var coupon = MakeActiveCoupon(couponId: 7, code: "EXPIRED", discountType: "FIXED_AMOUNT",
            discountValue: 20m, customerId: 23);
        coupon.StartTime = DateTime.UtcNow.AddDays(-10);
        coupon.EndTime = DateTime.UtcNow.AddDays(-5); // already expired
        _paymentRepoMock.Setup(r => r.GetCouponWithTypeAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(coupon);

        var dto = new CreatePaymentDTO
        {
            OrderId = 23,
            ReceivedAmount = 200m,
            PaymentMethod = "CASH",
            CouponIds = new List<long> { 7 }
        };
        var service = CreateService();

        // Act
        Func<Task> act = () => service.ProcessPaymentAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("*outside its valid period*");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "ProcessPaymentAsync")]
    public async Task ProcessPaymentAsync_WhenCouponUsageLimitReached_ThrowsInvalidOperation()
    {
        // Arrange
        var order = MakeValidOrder(orderId: 24, customerId: 24);
        SetupOrderRepo(order);
        SetupNoActivePromotions();
        SetupAddPayment();

        var coupon = MakeActiveCoupon(couponId: 8, code: "MAXED", discountType: "FIXED_AMOUNT",
            discountValue: 20m, customerId: 24, maxUsage: 5, usedCount: 5); // used up
        _paymentRepoMock.Setup(r => r.GetCouponWithTypeAsync(8, It.IsAny<CancellationToken>()))
            .ReturnsAsync(coupon);

        var dto = new CreatePaymentDTO
        {
            OrderId = 24,
            ReceivedAmount = 200m,
            PaymentMethod = "CASH",
            CouponIds = new List<long> { 8 }
        };
        var service = CreateService();

        // Act
        Func<Task> act = () => service.ProcessPaymentAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("*usage limit has been reached*");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "ProcessPaymentAsync")]
    public async Task ProcessPaymentAsync_WhenPromotionWithMinOrderRule_SkipsIfBelowMinimum()
    {
        // Arrange — subtotal 200, rule requires minOrderValue = 500
        var order = MakeValidOrder(orderId: 25, customerId: 25);
        SetupOrderRepo(order);
        SetupAddPayment();

        var promotion = new Promotion
        {
            PromotionId = 3,
            PromoName = "Big Spender",
            DiscountValue = 50m,
            PromotionStatusLvId = ActivePromotionStatusId,
            StartTime = DateTime.UtcNow.AddDays(-1),
            EndTime = DateTime.UtcNow.AddDays(1),
            UsedCount = 0,
            TypeLv = new LookupValue { ValueId = 1, ValueCode = "FIXED_AMOUNT", ValueName = "Fixed", TypeId = 1, SortOrder = 1 },
            PromotionRules = new List<PromotionRule>
            {
                new() { RuleId = 1, PromotionId = 3, MinOrderValue = 500m }
            },
            PromotionTargets = new List<PromotionTarget>()
        };
        _paymentRepoMock.Setup(r => r.GetActivePromotionsAsync(
            It.IsAny<uint>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Promotion> { promotion });

        var dto = new CreatePaymentDTO
        {
            OrderId = 25,
            ReceivedAmount = 200m,
            PaymentMethod = "CASH"
        };
        var service = CreateService();

        // Act
        await service.ProcessPaymentAsync(dto);

        // Assert — promotion skipped, no discount
        order.TotalAmount.Should().Be(200m);
        order.OrderPromotions.Should().BeEmpty();
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "ProcessPaymentAsync")]
    public async Task ProcessPaymentAsync_WhenOrderNotFound_ThrowsKeyNotFound()
    {
        // Arrange
        _paymentRepoMock.Setup(r => r.GetOrderForPaymentAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

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
        var order = MakeValidOrder(orderId: 7, customerId: 7, orderStatusLvId: CancelledOrderStatusId);
        SetupOrderRepo(order);

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
        var order = MakeValidOrder(orderId: 8, customerId: 8, hasPriorPayment: true);
        SetupOrderRepo(order);

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
        var order = MakeValidOrder(orderId: 9, customerId: 9);
        SetupOrderRepo(order);
        SetupNoActivePromotions();

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
    [Trait("Type", "Abnormal")]
    [Trait("Method", "ProcessPaymentAsync")]
    public async Task ProcessPaymentAsync_WhenCouponNotFound_ThrowsInvalidOperation()
    {
        // Arrange
        var order = MakeValidOrder(orderId: 26, customerId: 26);
        SetupOrderRepo(order);
        SetupNoActivePromotions();

        _paymentRepoMock.Setup(r => r.GetCouponWithTypeAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Coupon?)null);

        var dto = new CreatePaymentDTO
        {
            OrderId = 26,
            ReceivedAmount = 200m,
            PaymentMethod = "CASH",
            CouponIds = new List<long> { 999 }
        };
        var service = CreateService();

        // Act
        Func<Task> act = () => service.ProcessPaymentAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("*Coupon*not found*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "ProcessPaymentAsync")]
    public async Task ProcessPaymentAsync_WhenExceptionThrown_RollsBackTransaction()
    {
        // Arrange — trigger insufficient amount after transaction begins
        var order = MakeValidOrder(orderId: 27, customerId: 27);
        SetupOrderRepo(order);
        SetupNoActivePromotions();

        var dto = new CreatePaymentDTO
        {
            OrderId = 27,
            ReceivedAmount = 1m, // will fail validation
            PaymentMethod = "CASH"
        };
        var service = CreateService();

        // Act
        Func<Task> act = () => service.ProcessPaymentAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        _unitOfWorkMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
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
