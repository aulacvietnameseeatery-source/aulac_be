using Core.DTO.General;
using Core.DTO.Promotion;
using Core.Entity;
using Core.Enum;
using LookupTypeEnum = Core.Enum.LookupType;
using Core.Interface.Repo;
using Core.Interface.Service.Entity;
using Core.Service;
using FluentAssertions;
using Moq;

namespace Tests.Services;

/// <summary>
/// Unit Test — PromotionService
/// Code Module : Core/Service/PromotionService.cs
/// Method      : GetPromotionsAsync, CreatePromotionAsync, UpdatePromotionAsync,
///               GetPromotionByIdAsync, GetPromotionDetailAsync, DisablePromotionAsync,
///               ActivatePromotionAsync, GetAvailablePromotionsAsync, DeletePromotionAsync
/// Created By  : quantm
/// Executed By : quantm
/// Test Req.   : Verify promotion management business logic including listing, creating,
///               updating, retrieving, enabling/disabling promotions, calculating available
///               promotions for orders, and deleting promotions with dependency checks.
/// </summary>
public class PromotionServiceTests
{
    // ── Mocks ──
    private readonly Mock<IPromotionRepository> _promoRepoMock = new();
    private readonly Mock<IOrderRepository> _orderRepoMock = new();
    private readonly Mock<ILookupResolver> _lookupResolverMock = new();

    // ── Factory ──
    private PromotionService CreateService() => new(
        _promoRepoMock.Object,
        _orderRepoMock.Object,
        _lookupResolverMock.Object);

    // ── Constants for lookup IDs ──
    private const uint PercentTypeLvId = 100u;
    private const uint FixedAmountTypeLvId = 101u;
    private const uint ScheduledStatusLvId = 200u;
    private const uint ActiveStatusLvId = 201u;
    private const uint ExpiredStatusLvId = 202u;
    private const uint DisabledStatusLvId = 203u;

    // ── Helpers ──
    private void SetupLookupResolver()
    {
        _lookupResolverMock.Setup(r => r.GetIdAsync(
            (ushort)LookupTypeEnum.PromotionType, PromotionTypeCode.PERCENT, It.IsAny<CancellationToken>()))
            .ReturnsAsync(PercentTypeLvId);

        _lookupResolverMock.Setup(r => r.GetIdAsync(
            (ushort)LookupTypeEnum.PromotionType, PromotionTypeCode.FIXED_AMOUNT, It.IsAny<CancellationToken>()))
            .ReturnsAsync(FixedAmountTypeLvId);

        _lookupResolverMock.Setup(r => r.GetIdAsync(
            (ushort)LookupTypeEnum.PromotionStatus, PromotionStatusCode.SCHEDULED, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ScheduledStatusLvId);

        _lookupResolverMock.Setup(r => r.GetIdAsync(
            (ushort)LookupTypeEnum.PromotionStatus, PromotionStatusCode.ACTIVE, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ActiveStatusLvId);

        _lookupResolverMock.Setup(r => r.GetIdAsync(
            (ushort)LookupTypeEnum.PromotionStatus, PromotionStatusCode.EXPIRED, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ExpiredStatusLvId);

        _lookupResolverMock.Setup(r => r.GetIdAsync(
            (ushort)LookupTypeEnum.PromotionStatus, PromotionStatusCode.DISABLED, It.IsAny<CancellationToken>()))
            .ReturnsAsync(DisabledStatusLvId);
    }

    private static Promotion MakePromotion(
        long id = 1,
        string promoCode = "PROMO10",
        string promoName = "10% Off",
        decimal discountValue = 10m,
        int? maxUsage = 100,
        int? usedCount = 0,
        uint typeLvId = PercentTypeLvId,
        uint statusLvId = ActiveStatusLvId,
        DateTime? startTime = null,
        DateTime? endTime = null) => new()
        {
            PromotionId = id,
            PromoCode = promoCode,
            PromoName = promoName,
            Description = "Test promotion",
            DiscountValue = discountValue,
            MaxUsage = maxUsage,
            UsedCount = usedCount,
            TypeLvId = typeLvId,
            PromotionStatusLvId = statusLvId,
            StartTime = startTime ?? DateTime.UtcNow.AddDays(-1),
            EndTime = endTime ?? DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow,
            TypeLv = new LookupValue { ValueCode = typeLvId == PercentTypeLvId ? "PERCENT" : "FIXED_AMOUNT" },
            PromotionStatusLv = new LookupValue { ValueCode = statusLvId == ActiveStatusLvId ? "ACTIVE" : "SCHEDULED" },
            PromotionRules = new List<PromotionRule>(),
            PromotionTargets = new List<PromotionTarget>(),
            OrderPromotions = new List<OrderPromotion>()
        };

    private static PromotionDto MakePromotionDto(
        long? promotionId = null,
        string promoCode = "PROMO10",
        string promoName = "10% Off",
        decimal discountValue = 10m,
        PromotionTypeCode type = PromotionTypeCode.PERCENT,
        int maxUsage = 100,
        DateTime? start = null,
        DateTime? end = null) => new()
        {
            PromotionId = promotionId,
            PromoCode = promoCode,
            PromoName = promoName,
            Description = "Test promotion",
            DiscountValue = discountValue,
            Type = type,
            MaxUsage = maxUsage,
            StartTime = start ?? DateTime.UtcNow.AddDays(-1),
            EndTime = end ?? DateTime.UtcNow.AddDays(30),
            PromotionRules = new List<PromotionRuleDto>(),
            PromotionTargets = new List<PromotionTargetDto>()
        };

    private static Order MakeOrderWithItems(
        long orderId = 1,
        decimal totalAmount = 500m,
        List<OrderItem>? items = null) => new()
        {
            OrderId = orderId,
            TotalAmount = totalAmount,
            OrderItems = items ?? new List<OrderItem>
            {
                new()
                {
                    OrderItemId = 1,
                    DishId = 10,
                    Quantity = 2,
                    Price = 100m,
                    Dish = new Dish { DishId = 10, DishName = "Pho", CategoryId = 1 }
                },
                new()
                {
                    OrderItemId = 2,
                    DishId = 20,
                    Quantity = 3,
                    Price = 100m,
                    Dish = new Dish { DishId = 20, DishName = "Bun Bo", CategoryId = 2 }
                }
            }
        };

    // ══════════════════════════════════════════════════════════════════
    // GetPromotionsAsync
    // ══════════════════════════════════════════════════════════════════

    #region GetPromotionsAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetPromotionsAsync")]
    public async Task GetPromotionsAsync_WhenValidQuery_ReturnsPagedResult()
    {
        // Arrange
        var query = new PromotionListQueryDTO { PageIndex = 1, PageSize = 10 };
        var expected = new PagedResultDTO<PromotionListDTO>
        {
            PageData = new List<PromotionListDTO>
            {
                new() { PromotionId = 1, PromoCode = "PROMO10", PromoName = "10% Off" }
            },
            PageIndex = 1,
            PageSize = 10,
            TotalCount = 1
        };
        _promoRepoMock.Setup(r => r.GetPromotionsAsync(query, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(expected);
        var service = CreateService();

        // Act
        var result = await service.GetPromotionsAsync(query, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(expected);
        _promoRepoMock.Verify(r => r.GetPromotionsAsync(query, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetPromotionsAsync")]
    public async Task GetPromotionsAsync_WhenNoPromotions_ReturnsEmptyPage()
    {
        // Arrange
        var query = new PromotionListQueryDTO { PageIndex = 1, PageSize = 10 };
        var expected = new PagedResultDTO<PromotionListDTO>
        {
            PageData = new List<PromotionListDTO>(),
            PageIndex = 1,
            PageSize = 10,
            TotalCount = 0
        };
        _promoRepoMock.Setup(r => r.GetPromotionsAsync(query, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(expected);
        var service = CreateService();

        // Act
        var result = await service.GetPromotionsAsync(query, CancellationToken.None);

        // Assert
        result.PageData.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    #endregion

    // ══════════════════════════════════════════════════════════════════
    // CreatePromotionAsync
    // ══════════════════════════════════════════════════════════════════

    #region CreatePromotionAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "CreatePromotionAsync")]
    public async Task CreatePromotionAsync_WhenValidPercentPromo_CreatesAndReturnsId()
    {
        // Arrange
        SetupLookupResolver();
        var request = MakePromotionDto(promoCode: "NEW10", promoName: "New Promo", discountValue: 10m);
        _promoRepoMock.Setup(r => r.AddAsync(It.IsAny<Promotion>(), It.IsAny<CancellationToken>()))
                      .Returns(Task.CompletedTask)
                      .Callback<Promotion, CancellationToken>((p, _) => p.PromotionId = 50);
        _promoRepoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                      .Returns(Task.CompletedTask);
        var service = CreateService();

        // Act
        var result = await service.CreatePromotionAsync(request, CancellationToken.None);

        // Assert
        result.Should().Be(50);
        _promoRepoMock.Verify(r => r.AddAsync(It.Is<Promotion>(p =>
            p.PromoCode == "NEW10" &&
            p.PromoName == "New Promo" &&
            p.DiscountValue == 10m &&
            p.TypeLvId == PercentTypeLvId
        ), It.IsAny<CancellationToken>()), Times.Once);
        _promoRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "CreatePromotionAsync")]
    public async Task CreatePromotionAsync_WhenValidFixedAmountPromo_CreatesSuccessfully()
    {
        // Arrange
        SetupLookupResolver();
        var request = MakePromotionDto(
            promoCode: "FIXED50",
            promoName: "50 CHF Off",
            discountValue: 50m,
            type: PromotionTypeCode.FIXED_AMOUNT);
        _promoRepoMock.Setup(r => r.AddAsync(It.IsAny<Promotion>(), It.IsAny<CancellationToken>()))
                      .Returns(Task.CompletedTask);
        _promoRepoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                      .Returns(Task.CompletedTask);
        var service = CreateService();

        // Act
        var result = await service.CreatePromotionAsync(request, CancellationToken.None);

        // Assert
        _promoRepoMock.Verify(r => r.AddAsync(It.Is<Promotion>(p =>
            p.TypeLvId == FixedAmountTypeLvId
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "CreatePromotionAsync")]
    public async Task CreatePromotionAsync_WhenHasRulesAndTargets_AddsThemToPromotion()
    {
        // Arrange
        SetupLookupResolver();
        var request = MakePromotionDto();
        request.PromotionRules.Add(new PromotionRuleDto { MinOrderValue = 100m, MinQuantity = 2 });
        request.PromotionTargets.Add(new PromotionTargetDto { DishId = 10 });
        _promoRepoMock.Setup(r => r.AddAsync(It.IsAny<Promotion>(), It.IsAny<CancellationToken>()))
                      .Returns(Task.CompletedTask);
        _promoRepoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                      .Returns(Task.CompletedTask);
        var service = CreateService();

        // Act
        await service.CreatePromotionAsync(request, CancellationToken.None);

        // Assert
        _promoRepoMock.Verify(r => r.AddAsync(It.Is<Promotion>(p =>
            p.PromotionRules.Count == 1 &&
            p.PromotionTargets.Count == 1
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CreatePromotionAsync")]
    public async Task CreatePromotionAsync_WhenPromoCodeIsEmpty_ThrowsInvalidOperation()
    {
        // Arrange
        var request = MakePromotionDto(promoCode: "");
        var service = CreateService();

        // Act
        Func<Task> act = () => service.CreatePromotionAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("*PromoCode*required*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CreatePromotionAsync")]
    public async Task CreatePromotionAsync_WhenPromoNameIsEmpty_ThrowsInvalidOperation()
    {
        // Arrange
        var request = MakePromotionDto(promoName: "");
        var service = CreateService();

        // Act
        Func<Task> act = () => service.CreatePromotionAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("*PromoName*required*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CreatePromotionAsync")]
    public async Task CreatePromotionAsync_WhenEndTimeBeforeStartTime_ThrowsInvalidOperation()
    {
        // Arrange
        var request = MakePromotionDto(
            start: DateTime.UtcNow.AddDays(10),
            end: DateTime.UtcNow.AddDays(5));
        var service = CreateService();

        // Act
        Func<Task> act = () => service.CreatePromotionAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("*EndTime must be greater than StartTime*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CreatePromotionAsync")]
    public async Task CreatePromotionAsync_WhenPercentGreaterThan100_ThrowsInvalidOperation()
    {
        // Arrange
        var request = MakePromotionDto(discountValue: 150m, type: PromotionTypeCode.PERCENT);
        var service = CreateService();

        // Act
        Func<Task> act = () => service.CreatePromotionAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("*Percent must be between 1 and 100*");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "CreatePromotionAsync")]
    public async Task CreatePromotionAsync_WhenPercentIsZero_ThrowsInvalidOperation()
    {
        // Arrange
        var request = MakePromotionDto(discountValue: 0m, type: PromotionTypeCode.PERCENT);
        var service = CreateService();

        // Act
        Func<Task> act = () => service.CreatePromotionAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("*Percent must be between 1 and 100*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CreatePromotionAsync")]
    public async Task CreatePromotionAsync_WhenFixedAmountIsZeroOrNegative_ThrowsInvalidOperation()
    {
        // Arrange
        var request = MakePromotionDto(discountValue: 0m, type: PromotionTypeCode.FIXED_AMOUNT);
        var service = CreateService();

        // Act
        Func<Task> act = () => service.CreatePromotionAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("*DiscountValue must be greater than 0*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CreatePromotionAsync")]
    public async Task CreatePromotionAsync_WhenMaxUsageIsNegative_ThrowsInvalidOperation()
    {
        // Arrange
        var request = MakePromotionDto();
        request.MaxUsage = -1;
        var service = CreateService();

        // Act
        Func<Task> act = () => service.CreatePromotionAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("*MaxUsage must be greater than 0*");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "CreatePromotionAsync")]
    public async Task CreatePromotionAsync_WhenMaxUsageIsZero_StoresAsNull()
    {
        // Arrange
        SetupLookupResolver();
        var request = MakePromotionDto();
        request.MaxUsage = 0;
        _promoRepoMock.Setup(r => r.AddAsync(It.IsAny<Promotion>(), It.IsAny<CancellationToken>()))
                      .Returns(Task.CompletedTask);
        _promoRepoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                      .Returns(Task.CompletedTask);
        var service = CreateService();

        // Act
        await service.CreatePromotionAsync(request, CancellationToken.None);

        // Assert
        _promoRepoMock.Verify(r => r.AddAsync(It.Is<Promotion>(p =>
            p.MaxUsage == null
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    // ══════════════════════════════════════════════════════════════════
    // UpdatePromotionAsync
    // ══════════════════════════════════════════════════════════════════

    #region UpdatePromotionAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "UpdatePromotionAsync")]
    public async Task UpdatePromotionAsync_WhenValidRequest_UpdatesPromotion()
    {
        // Arrange
        SetupLookupResolver();
        var existingPromo = MakePromotion(id: 1);
        _promoRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(existingPromo);
        _promoRepoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                      .Returns(Task.CompletedTask);

        var request = MakePromotionDto(promotionId: 1, promoCode: "UPDATED", promoName: "Updated Promo", discountValue: 20m);
        var service = CreateService();

        // Act
        await service.UpdatePromotionAsync(request, CancellationToken.None);

        // Assert
        existingPromo.PromoCode.Should().Be("UPDATED");
        existingPromo.PromoName.Should().Be("Updated Promo");
        existingPromo.DiscountValue.Should().Be(20m);
        _promoRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "UpdatePromotionAsync")]
    public async Task UpdatePromotionAsync_WhenStatusIsDisabled_SetDisabledStatus()
    {
        // Arrange
        SetupLookupResolver();
        var existingPromo = MakePromotion(id: 1);
        _promoRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(existingPromo);
        _promoRepoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                      .Returns(Task.CompletedTask);

        var request = MakePromotionDto(promotionId: 1);
        request.PromotionStatus = PromotionStatusCode.DISABLED;
        var service = CreateService();

        // Act
        await service.UpdatePromotionAsync(request, CancellationToken.None);

        // Assert
        existingPromo.PromotionStatusLvId.Should().Be(DisabledStatusLvId);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "UpdatePromotionAsync")]
    public async Task UpdatePromotionAsync_WhenHasNewRulesAndTargets_ReplacesOldOnes()
    {
        // Arrange
        SetupLookupResolver();
        var existingPromo = MakePromotion(id: 1);
        existingPromo.PromotionRules.Add(new PromotionRule { RuleId = 1, MinOrderValue = 50m });
        existingPromo.PromotionTargets.Add(new PromotionTarget { TargetId = 1, DishId = 5 });
        _promoRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(existingPromo);
        _promoRepoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                      .Returns(Task.CompletedTask);

        var request = MakePromotionDto(promotionId: 1);
        request.PromotionRules.Add(new PromotionRuleDto { MinOrderValue = 200m });
        request.PromotionTargets.Add(new PromotionTargetDto { CategoryId = 3 });
        var service = CreateService();

        // Act
        await service.UpdatePromotionAsync(request, CancellationToken.None);

        // Assert
        _promoRepoMock.Verify(r => r.RemoveRules(It.IsAny<IEnumerable<PromotionRule>>()), Times.Once);
        _promoRepoMock.Verify(r => r.RemoveTargets(It.IsAny<IEnumerable<PromotionTarget>>()), Times.Once);
        existingPromo.PromotionRules.Should().HaveCount(1);
        existingPromo.PromotionTargets.Should().HaveCount(1);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "UpdatePromotionAsync")]
    public async Task UpdatePromotionAsync_WhenPromotionIdIsNull_ThrowsException()
    {
        // Arrange
        var request = MakePromotionDto(promotionId: null);
        var service = CreateService();

        // Act
        Func<Task> act = () => service.UpdatePromotionAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<Exception>()
                 .WithMessage("*PromotionId is required*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "UpdatePromotionAsync")]
    public async Task UpdatePromotionAsync_WhenPromotionNotFound_ThrowsException()
    {
        // Arrange
        _promoRepoMock.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
                      .ReturnsAsync((Promotion?)null);
        var request = MakePromotionDto(promotionId: 999);
        var service = CreateService();

        // Act
        Func<Task> act = () => service.UpdatePromotionAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<Exception>()
                 .WithMessage("*Promotion not found*");
    }

    #endregion

    // ══════════════════════════════════════════════════════════════════
    // GetPromotionByIdAsync
    // ══════════════════════════════════════════════════════════════════

    #region GetPromotionByIdAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetPromotionByIdAsync")]
    public async Task GetPromotionByIdAsync_WhenExists_ReturnsMappedDto()
    {
        // Arrange
        var promo = MakePromotion(id: 1, promoCode: "CODE1", promoName: "Name1");
        promo.TypeLv = new LookupValue { ValueCode = "PERCENT" };
        promo.PromotionStatusLv = new LookupValue { ValueCode = "ACTIVE" };
        promo.PromotionRules.Add(new PromotionRule { RuleId = 1, MinOrderValue = 100m });
        promo.PromotionTargets.Add(new PromotionTarget { TargetId = 1, DishId = 10 });
        _promoRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(promo);
        var service = CreateService();

        // Act
        var result = await service.GetPromotionByIdAsync(1, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.PromotionId.Should().Be(1);
        result.PromoCode.Should().Be("CODE1");
        result.PromoName.Should().Be("Name1");
        result.Type.Should().Be(PromotionTypeCode.PERCENT);
        result.PromotionStatus.Should().Be(PromotionStatusCode.ACTIVE);
        result.PromotionRules.Should().HaveCount(1);
        result.PromotionTargets.Should().HaveCount(1);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "GetPromotionByIdAsync")]
    public async Task GetPromotionByIdAsync_WhenNotFound_ThrowsInvalidOperation()
    {
        // Arrange
        _promoRepoMock.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
                      .ReturnsAsync((Promotion?)null);
        var service = CreateService();

        // Act
        Func<Task> act = () => service.GetPromotionByIdAsync(999, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("*Promotion not found*");
    }

    #endregion

    // ══════════════════════════════════════════════════════════════════
    // GetPromotionDetailAsync
    // ══════════════════════════════════════════════════════════════════

    #region GetPromotionDetailAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetPromotionDetailAsync")]
    public async Task GetPromotionDetailAsync_WhenExists_ReturnsDetailDto()
    {
        // Arrange
        var promo = MakePromotion(id: 1, promoCode: "DETAIL1", usedCount: 5);
        promo.TypeLv = new LookupValue { ValueCode = "PERCENT" };
        promo.PromotionStatusLv = new LookupValue { ValueCode = "ACTIVE" };
        _promoRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(promo);
        var service = CreateService();

        // Act
        var result = await service.GetPromotionDetailAsync(1, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.PromotionId.Should().Be(1);
        result.PromoCode.Should().Be("DETAIL1");
        result.UsedCount.Should().Be(5);
        result.Type.Should().Be(PromotionTypeCode.PERCENT);
        result.PromotionStatus.Should().Be(PromotionStatusCode.ACTIVE);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "GetPromotionDetailAsync")]
    public async Task GetPromotionDetailAsync_WhenNotFound_ThrowsInvalidOperation()
    {
        // Arrange
        _promoRepoMock.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
                      .ReturnsAsync((Promotion?)null);
        var service = CreateService();

        // Act
        Func<Task> act = () => service.GetPromotionDetailAsync(999, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("*Promotion not found*");
    }

    #endregion

    // ══════════════════════════════════════════════════════════════════
    // DisablePromotionAsync
    // ══════════════════════════════════════════════════════════════════

    #region DisablePromotionAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "DisablePromotionAsync")]
    public async Task DisablePromotionAsync_WhenExists_SetsDisabledStatus()
    {
        // Arrange
        SetupLookupResolver();
        var promo = MakePromotion(id: 1, statusLvId: ActiveStatusLvId);
        _promoRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(promo);
        _promoRepoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                      .Returns(Task.CompletedTask);
        var service = CreateService();

        // Act
        await service.DisablePromotionAsync(1, CancellationToken.None);

        // Assert
        promo.PromotionStatusLvId.Should().Be(DisabledStatusLvId);
        _promoRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "DisablePromotionAsync")]
    public async Task DisablePromotionAsync_WhenNotFound_ThrowsInvalidOperation()
    {
        // Arrange
        _promoRepoMock.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
                      .ReturnsAsync((Promotion?)null);
        var service = CreateService();

        // Act
        Func<Task> act = () => service.DisablePromotionAsync(999, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("*Promotion not found*");
    }

    #endregion

    // ══════════════════════════════════════════════════════════════════
    // ActivatePromotionAsync
    // ══════════════════════════════════════════════════════════════════

    #region ActivatePromotionAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "ActivatePromotionAsync")]
    public async Task ActivatePromotionAsync_WhenCurrentlyWithinPeriod_SetsActiveStatus()
    {
        // Arrange
        SetupLookupResolver();
        var promo = MakePromotion(
            id: 1,
            startTime: DateTime.UtcNow.AddDays(-5),
            endTime: DateTime.UtcNow.AddDays(5),
            statusLvId: DisabledStatusLvId);
        _promoRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(promo);
        _promoRepoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                      .Returns(Task.CompletedTask);
        var service = CreateService();

        // Act
        await service.ActivatePromotionAsync(1, CancellationToken.None);

        // Assert
        promo.PromotionStatusLvId.Should().Be(ActiveStatusLvId);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "ActivatePromotionAsync")]
    public async Task ActivatePromotionAsync_WhenBeforeStartTime_SetsScheduledStatus()
    {
        // Arrange
        SetupLookupResolver();
        var promo = MakePromotion(
            id: 1,
            startTime: DateTime.UtcNow.AddDays(5),
            endTime: DateTime.UtcNow.AddDays(30),
            statusLvId: DisabledStatusLvId);
        _promoRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(promo);
        _promoRepoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                      .Returns(Task.CompletedTask);
        var service = CreateService();

        // Act
        await service.ActivatePromotionAsync(1, CancellationToken.None);

        // Assert
        promo.PromotionStatusLvId.Should().Be(ScheduledStatusLvId);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "ActivatePromotionAsync")]
    public async Task ActivatePromotionAsync_WhenAfterEndTime_SetsExpiredStatus()
    {
        // Arrange
        SetupLookupResolver();
        var promo = MakePromotion(
            id: 1,
            startTime: DateTime.UtcNow.AddDays(-30),
            endTime: DateTime.UtcNow.AddDays(-1),
            statusLvId: DisabledStatusLvId);
        _promoRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(promo);
        _promoRepoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                      .Returns(Task.CompletedTask);
        var service = CreateService();

        // Act
        await service.ActivatePromotionAsync(1, CancellationToken.None);

        // Assert
        promo.PromotionStatusLvId.Should().Be(ExpiredStatusLvId);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "ActivatePromotionAsync")]
    public async Task ActivatePromotionAsync_WhenNotFound_ThrowsInvalidOperation()
    {
        // Arrange
        _promoRepoMock.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
                      .ReturnsAsync((Promotion?)null);
        var service = CreateService();

        // Act
        Func<Task> act = () => service.ActivatePromotionAsync(999, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("*Promotion not found*");
    }

    #endregion

    // ══════════════════════════════════════════════════════════════════
    // GetAvailablePromotionsAsync
    // ══════════════════════════════════════════════════════════════════

    #region GetAvailablePromotionsAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetAvailablePromotionsAsync")]
    public async Task GetAvailablePromotionsAsync_WhenGeneralPercentPromoMatches_ReturnsWithDiscount()
    {
        // Arrange
        var order = MakeOrderWithItems(orderId: 1, totalAmount: 500m);
        _orderRepoMock.Setup(r => r.GetOrderWithItemsAsync(1, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(order);

        var promo = MakePromotion(id: 10, discountValue: 10m, typeLvId: PercentTypeLvId);
        promo.TypeLv = new LookupValue { ValueCode = "PERCENT" };
        _promoRepoMock.Setup(r => r.GetActivePromotionsAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync(new List<Promotion> { promo });
        var service = CreateService();

        // Act
        var result = await service.GetAvailablePromotionsAsync(1, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].PromotionId.Should().Be(10);
        result[0].EstimatedDiscount.Should().Be(50m); // 500 * 10%
        result[0].FinalAmount.Should().Be(450m); // 500 - 50
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetAvailablePromotionsAsync")]
    public async Task GetAvailablePromotionsAsync_WhenFixedAmountPromo_ReturnsCorrectDiscount()
    {
        // Arrange
        var order = MakeOrderWithItems(orderId: 1, totalAmount: 500m);
        _orderRepoMock.Setup(r => r.GetOrderWithItemsAsync(1, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(order);

        var promo = MakePromotion(id: 10, discountValue: 100m, typeLvId: FixedAmountTypeLvId);
        promo.TypeLv = new LookupValue { ValueCode = "FIXED_AMOUNT" };
        _promoRepoMock.Setup(r => r.GetActivePromotionsAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync(new List<Promotion> { promo });
        var service = CreateService();

        // Act
        var result = await service.GetAvailablePromotionsAsync(1, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].EstimatedDiscount.Should().Be(100m);
        result[0].FinalAmount.Should().Be(400m);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetAvailablePromotionsAsync")]
    public async Task GetAvailablePromotionsAsync_WhenRuleMinOrderNotMet_ExcludesPromo()
    {
        // Arrange
        var order = MakeOrderWithItems(orderId: 1, totalAmount: 50m);
        _orderRepoMock.Setup(r => r.GetOrderWithItemsAsync(1, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(order);

        var promo = MakePromotion(id: 10, discountValue: 10m);
        promo.TypeLv = new LookupValue { ValueCode = "PERCENT" };
        promo.PromotionRules.Add(new PromotionRule { RuleId = 1, MinOrderValue = 200m });
        _promoRepoMock.Setup(r => r.GetActivePromotionsAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync(new List<Promotion> { promo });
        var service = CreateService();

        // Act
        var result = await service.GetAvailablePromotionsAsync(1, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetAvailablePromotionsAsync")]
    public async Task GetAvailablePromotionsAsync_WhenRuleMinQuantityNotMet_ExcludesPromo()
    {
        // Arrange
        var order = MakeOrderWithItems(orderId: 1, totalAmount: 500m);
        _orderRepoMock.Setup(r => r.GetOrderWithItemsAsync(1, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(order);

        var promo = MakePromotion(id: 10, discountValue: 10m);
        promo.TypeLv = new LookupValue { ValueCode = "PERCENT" };
        promo.PromotionRules.Add(new PromotionRule { RuleId = 1, MinQuantity = 100 }); // order has 5 items total
        _promoRepoMock.Setup(r => r.GetActivePromotionsAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync(new List<Promotion> { promo });
        var service = CreateService();

        // Act
        var result = await service.GetAvailablePromotionsAsync(1, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetAvailablePromotionsAsync")]
    public async Task GetAvailablePromotionsAsync_WhenRequiredDishMissing_ExcludesPromo()
    {
        // Arrange
        var order = MakeOrderWithItems(orderId: 1, totalAmount: 500m);
        _orderRepoMock.Setup(r => r.GetOrderWithItemsAsync(1, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(order);

        var promo = MakePromotion(id: 10, discountValue: 10m);
        promo.TypeLv = new LookupValue { ValueCode = "PERCENT" };
        promo.PromotionRules.Add(new PromotionRule { RuleId = 1, RequiredDishId = 999 }); // dish not in order
        _promoRepoMock.Setup(r => r.GetActivePromotionsAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync(new List<Promotion> { promo });
        var service = CreateService();

        // Act
        var result = await service.GetAvailablePromotionsAsync(1, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetAvailablePromotionsAsync")]
    public async Task GetAvailablePromotionsAsync_WhenNoActivePromotions_ReturnsEmptyList()
    {
        // Arrange
        var order = MakeOrderWithItems(orderId: 1, totalAmount: 500m);
        _orderRepoMock.Setup(r => r.GetOrderWithItemsAsync(1, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(order);
        _promoRepoMock.Setup(r => r.GetActivePromotionsAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync(new List<Promotion>());
        var service = CreateService();

        // Act
        var result = await service.GetAvailablePromotionsAsync(1, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "GetAvailablePromotionsAsync")]
    public async Task GetAvailablePromotionsAsync_WhenOrderNotFound_ThrowsException()
    {
        // Arrange
        _orderRepoMock.Setup(r => r.GetOrderWithItemsAsync(999, It.IsAny<CancellationToken>()))
                      .ReturnsAsync((Order?)null);
        var service = CreateService();

        // Act
        Func<Task> act = () => service.GetAvailablePromotionsAsync(999, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<Exception>()
                 .WithMessage("*Order not found*");
    }

    #endregion

    // ══════════════════════════════════════════════════════════════════
    // DeletePromotionAsync
    // ══════════════════════════════════════════════════════════════════

    #region DeletePromotionAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "DeletePromotionAsync")]
    public async Task DeletePromotionAsync_WhenNoDependencies_DeletesSuccessfully()
    {
        // Arrange
        var promo = MakePromotion(id: 1);
        promo.OrderPromotions = new List<OrderPromotion>();
        promo.PromotionRules = new List<PromotionRule>();
        promo.PromotionTargets = new List<PromotionTarget>();
        _promoRepoMock.Setup(r => r.GetByIdWithRelationsAsync(1, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(promo);
        _promoRepoMock.Setup(r => r.DeleteAsync(promo, It.IsAny<CancellationToken>()))
                      .Returns(Task.CompletedTask);
        _promoRepoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                      .Returns(Task.CompletedTask);
        var service = CreateService();

        // Act
        await service.DeletePromotionAsync(1, CancellationToken.None);

        // Assert
        _promoRepoMock.Verify(r => r.DeleteAsync(promo, It.IsAny<CancellationToken>()), Times.Once);
        _promoRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "DeletePromotionAsync")]
    public async Task DeletePromotionAsync_WhenHasRulesAndTargets_RemovesThemBeforeDelete()
    {
        // Arrange
        var promo = MakePromotion(id: 1);
        promo.OrderPromotions = new List<OrderPromotion>();
        promo.PromotionRules = new List<PromotionRule> { new() { RuleId = 1 } };
        promo.PromotionTargets = new List<PromotionTarget> { new() { TargetId = 1 } };
        _promoRepoMock.Setup(r => r.GetByIdWithRelationsAsync(1, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(promo);
        _promoRepoMock.Setup(r => r.DeleteAsync(promo, It.IsAny<CancellationToken>()))
                      .Returns(Task.CompletedTask);
        _promoRepoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                      .Returns(Task.CompletedTask);
        var service = CreateService();

        // Act
        await service.DeletePromotionAsync(1, CancellationToken.None);

        // Assert
        _promoRepoMock.Verify(r => r.RemoveRules(It.IsAny<ICollection<PromotionRule>>()), Times.Once);
        _promoRepoMock.Verify(r => r.RemoveTargets(It.IsAny<ICollection<PromotionTarget>>()), Times.Once);
        _promoRepoMock.Verify(r => r.DeleteAsync(promo, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "DeletePromotionAsync")]
    public async Task DeletePromotionAsync_WhenNotFound_ThrowsKeyNotFound()
    {
        // Arrange
        _promoRepoMock.Setup(r => r.GetByIdWithRelationsAsync(999, It.IsAny<CancellationToken>()))
                      .ReturnsAsync((Promotion?)null);
        var service = CreateService();

        // Act
        Func<Task> act = () => service.DeletePromotionAsync(999, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
                 .WithMessage("*Promotion not found*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "DeletePromotionAsync")]
    public async Task DeletePromotionAsync_WhenUsedInOrders_ThrowsInvalidOperation()
    {
        // Arrange
        var promo = MakePromotion(id: 1);
        promo.OrderPromotions = new List<OrderPromotion>
        {
            new() { OrderPromotionId = 1, OrderId = 100, PromotionId = 1 }
        };
        _promoRepoMock.Setup(r => r.GetByIdWithRelationsAsync(1, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(promo);
        var service = CreateService();

        // Act
        Func<Task> act = () => service.DeletePromotionAsync(1, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("*already been used in orders*");
    }

    #endregion
}
