using Core.DTO.Coupon;
using Core.Entity;
using Core.Enum;
using Core.Interface.Repo;
using Core.Interface.Service.Entity;
using Core.Service;
using FluentAssertions;
using Moq;
using LookupTypeEnum = Core.Enum.LookupType;

namespace Tests.Services;

/// <summary>
/// Unit Test — CouponService
/// Code Module : Core/Service/CouponService.cs
/// Method      : GetCouponsAsync, GetCouponDetailAsync, CreateCouponAsync,
///               UpdateCouponAsync, DeleteCouponAsync
/// Created By  : quantm
/// Executed By : quantm
/// Test Req.   : Verify coupon management business logic including listing active coupons
///               with optional customer filtering, retrieving coupon details, creating coupons
///               with code normalization/uniqueness/date/discount validation, updating coupons
///               with used-vs-unused branching, and deleting unused coupons.
/// </summary>
public class CouponServiceTests
{
    // ── Mocks ──
    private readonly Mock<ICouponRepository> _couponRepoMock = new();
    private readonly Mock<ILookupResolver> _lookupResolverMock = new();

    // ── Lookup IDs ──
    private const uint ActiveStatusId = 500;
    private const uint ScheduledStatusId = 501;
    private const uint ExpiredStatusId = 502;
    private const uint FixedAmountTypeId = 600;
    private const uint PercentTypeId = 601;

    // ── Factory ──
    private CouponService CreateService() => new(
        _couponRepoMock.Object,
        _lookupResolverMock.Object);

    // ── Test Data Helpers ──
    private static LookupValue MakeTypeLv(string code = "FIXED_AMOUNT") => new()
    {
        ValueId = code == "PERCENT" ? PercentTypeId : FixedAmountTypeId,
        ValueCode = code,
        ValueName = code
    };

    private static LookupValue MakeStatusLv(string code = "ACTIVE") => new()
    {
        ValueId = code switch
        {
            "SCHEDULED" => ScheduledStatusId,
            "EXPIRED" => ExpiredStatusId,
            _ => ActiveStatusId
        },
        ValueCode = code,
        ValueName = code
    };

    private static Coupon MakeValidCoupon(
        long id = 1,
        string code = "SUMMER2025",
        string name = "Summer Discount",
        decimal discount = 50000,
        int? usedCount = 0,
        int? maxUsage = 100,
        long? customerId = null,
        string typeCode = "FIXED_AMOUNT",
        string statusCode = "ACTIVE",
        DateTime? start = null,
        DateTime? end = null) => new()
    {
        CouponId = id,
        CouponCode = code,
        CouponName = name,
        Description = "Test coupon",
        DiscountValue = discount,
        UsedCount = usedCount,
        MaxUsage = maxUsage,
        CustomerId = customerId,
        Customer = customerId.HasValue ? new Customer { CustomerId = customerId.Value, FullName = "Test Customer" } : null,
        StartTime = start ?? DateTime.UtcNow.AddDays(-1),
        EndTime = end ?? DateTime.UtcNow.AddDays(30),
        TypeLvId = typeCode == "PERCENT" ? PercentTypeId : FixedAmountTypeId,
        TypeLv = MakeTypeLv(typeCode),
        CouponStatusLvId = statusCode switch
        {
            "SCHEDULED" => ScheduledStatusId,
            "EXPIRED" => ExpiredStatusId,
            _ => ActiveStatusId
        },
        CouponStatusLv = MakeStatusLv(statusCode),
        CreatedAt = DateTime.UtcNow.AddDays(-5)
    };

    private static CreateCouponRequest MakeValidCreateRequest() => new()
    {
        CouponCode = "NEWYEAR2025",
        CouponName = "New Year Discount",
        Description = "New year special",
        StartTime = DateTime.UtcNow.AddDays(-1),
        EndTime = DateTime.UtcNow.AddDays(30),
        DiscountValue = 20000,
        MaxUsage = 50,
        Type = "FIXED_AMOUNT"
    };

    private static UpdateCouponRequest MakeValidUpdateRequest() => new()
    {
        CouponCode = "UPDATED2025",
        CouponName = "Updated Coupon",
        Description = "Updated description",
        StartTime = DateTime.UtcNow.AddDays(-1),
        EndTime = DateTime.UtcNow.AddDays(60),
        DiscountValue = 30000,
        MaxUsage = 200,
        Type = "FIXED_AMOUNT"
    };

    // ══════════════════════════════════════════════════════════════════
    //  GetCouponsAsync
    // ══════════════════════════════════════════════════════════════════

    #region GetCouponsAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetCouponsAsync")]
    public async Task GetCouponsAsync_WhenNoCustomerId_ReturnsAllActiveCoupons()
    {
        // Arrange
        var coupons = new List<Coupon>
        {
            MakeValidCoupon(id: 1, code: "COUPON1"),
            MakeValidCoupon(id: 2, code: "COUPON2", customerId: 10)
        };
        _couponRepoMock
            .Setup(r => r.GetActiveCouponsAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(coupons);

        var svc = CreateService();

        // Act
        var result = await svc.GetCouponsAsync(null, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result[0].CouponCode.Should().Be("COUPON1");
        result[1].CouponCode.Should().Be("COUPON2");
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetCouponsAsync")]
    public async Task GetCouponsAsync_WhenCustomerId_ReturnsFilteredCoupons()
    {
        // Arrange
        var coupons = new List<Coupon>
        {
            MakeValidCoupon(id: 1, code: "GLOBAL", customerId: null),
            MakeValidCoupon(id: 2, code: "CUST10", customerId: 10),
            MakeValidCoupon(id: 3, code: "CUST20", customerId: 20)
        };
        _couponRepoMock
            .Setup(r => r.GetActiveCouponsAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(coupons);

        var svc = CreateService();

        // Act
        var result = await svc.GetCouponsAsync(10, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Select(c => c.CouponCode).Should().Contain("GLOBAL");
        result.Select(c => c.CouponCode).Should().Contain("CUST10");
        result.Select(c => c.CouponCode).Should().NotContain("CUST20");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetCouponsAsync")]
    public async Task GetCouponsAsync_WhenNoCoupons_ReturnsEmptyList()
    {
        // Arrange
        _couponRepoMock
            .Setup(r => r.GetActiveCouponsAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Coupon>());

        var svc = CreateService();

        // Act
        var result = await svc.GetCouponsAsync(null, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetCouponsAsync")]
    public async Task GetCouponsAsync_MapsAllDtoFieldsCorrectly()
    {
        // Arrange
        var coupon = MakeValidCoupon(id: 1, code: "MAPPED", customerId: 10);
        _couponRepoMock
            .Setup(r => r.GetActiveCouponsAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Coupon> { coupon });

        var svc = CreateService();

        // Act
        var result = await svc.GetCouponsAsync(null, CancellationToken.None);

        // Assert
        var dto = result.Single();
        dto.CouponId.Should().Be(1);
        dto.CouponCode.Should().Be("MAPPED");
        dto.CouponName.Should().Be("Summer Discount");
        dto.CustomerId.Should().Be(10);
        dto.CustomerName.Should().Be("Test Customer");
        dto.DiscountValue.Should().Be(50000);
        dto.MaxUsage.Should().Be(100);
        dto.UsedCount.Should().Be(0);
        dto.Type.Should().Be("FIXED_AMOUNT");
        dto.CouponStatus.Should().Be("ACTIVE");
    }

    #endregion

    // ══════════════════════════════════════════════════════════════════
    //  GetCouponDetailAsync
    // ══════════════════════════════════════════════════════════════════

    #region GetCouponDetailAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetCouponDetailAsync")]
    public async Task GetCouponDetailAsync_WhenCouponExists_ReturnsDetail()
    {
        // Arrange
        var coupon = MakeValidCoupon(id: 5, code: "DETAIL01");
        _couponRepoMock
            .Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(coupon);

        var svc = CreateService();

        // Act
        var result = await svc.GetCouponDetailAsync(5, CancellationToken.None);

        // Assert
        result.CouponId.Should().Be(5);
        result.CouponCode.Should().Be("DETAIL01");
        result.CouponName.Should().Be("Summer Discount");
        result.Description.Should().Be("Test coupon");
        result.DiscountValue.Should().Be(50000);
        result.MaxUsage.Should().Be(100);
        result.UsedCount.Should().Be(0);
        result.Type.Should().Be("FIXED_AMOUNT");
        result.CouponStatus.Should().Be("ACTIVE");
        result.CreatedAt.Should().NotBeNull();
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "GetCouponDetailAsync")]
    public async Task GetCouponDetailAsync_WhenCouponNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _couponRepoMock
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Coupon?)null);

        var svc = CreateService();

        // Act
        var act = () => svc.GetCouponDetailAsync(999, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*999*");
    }

    #endregion

    // ══════════════════════════════════════════════════════════════════
    //  CreateCouponAsync
    // ══════════════════════════════════════════════════════════════════

    #region CreateCouponAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "CreateCouponAsync")]
    public async Task CreateCouponAsync_WhenValidRequest_CreatesCouponAndReturnsDto()
    {
        // Arrange
        var request = MakeValidCreateRequest();

        _couponRepoMock
            .Setup(r => r.GetByCodeAsync("NEWYEAR2025", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Coupon?)null);

        _lookupResolverMock
            .Setup(r => r.GetIdAsync((ushort)LookupTypeEnum.CouponType, "FIXED_AMOUNT", It.IsAny<CancellationToken>()))
            .ReturnsAsync(FixedAmountTypeId);
        _lookupResolverMock
            .Setup(r => r.GetIdAsync((ushort)LookupTypeEnum.CouponStatus, It.IsAny<Enum>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ActiveStatusId);

        _couponRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<Coupon>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Coupon c, CancellationToken _) =>
            {
                c.CouponId = 100;
                c.TypeLv = MakeTypeLv("FIXED_AMOUNT");
                c.CouponStatusLv = MakeStatusLv("ACTIVE");
                return c;
            });

        var svc = CreateService();

        // Act
        var result = await svc.CreateCouponAsync(request, CancellationToken.None);

        // Assert
        result.CouponId.Should().Be(100);
        result.CouponCode.Should().Be("NEWYEAR2025");
        result.CouponName.Should().Be("New Year Discount");
        result.DiscountValue.Should().Be(20000);
        result.UsedCount.Should().Be(0);
        result.Type.Should().Be("FIXED_AMOUNT");
        result.CouponStatus.Should().Be("ACTIVE");
        _couponRepoMock.Verify(r => r.CreateAsync(It.IsAny<Coupon>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "CreateCouponAsync")]
    public async Task CreateCouponAsync_NormalizesCodeToUpperCase()
    {
        // Arrange
        var request = MakeValidCreateRequest();
        request.CouponCode = " new year 2025 ";

        _couponRepoMock
            .Setup(r => r.GetByCodeAsync("NEWYEAR2025", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Coupon?)null);
        _lookupResolverMock
            .Setup(r => r.GetIdAsync((ushort)LookupTypeEnum.CouponType, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FixedAmountTypeId);
        _lookupResolverMock
            .Setup(r => r.GetIdAsync((ushort)LookupTypeEnum.CouponStatus, It.IsAny<Enum>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ActiveStatusId);
        _couponRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<Coupon>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Coupon c, CancellationToken _) =>
            {
                c.CouponId = 101;
                c.TypeLv = MakeTypeLv();
                c.CouponStatusLv = MakeStatusLv();
                return c;
            });

        var svc = CreateService();

        // Act
        var result = await svc.CreateCouponAsync(request, CancellationToken.None);

        // Assert
        result.CouponCode.Should().Be("NEWYEAR2025");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CreateCouponAsync")]
    public async Task CreateCouponAsync_WhenCodeTooShort_ThrowsInvalidOperationException()
    {
        // Arrange
        var request = MakeValidCreateRequest();
        request.CouponCode = "AB";

        var svc = CreateService();

        // Act
        var act = () => svc.CreateCouponAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*at least 3 characters*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CreateCouponAsync")]
    public async Task CreateCouponAsync_WhenDuplicateCode_ThrowsInvalidOperationException()
    {
        // Arrange
        var request = MakeValidCreateRequest();
        _couponRepoMock
            .Setup(r => r.GetByCodeAsync("NEWYEAR2025", It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeValidCoupon());

        var svc = CreateService();

        // Act
        var act = () => svc.CreateCouponAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CreateCouponAsync")]
    public async Task CreateCouponAsync_WhenEndTimeBeforeStartTime_ThrowsInvalidOperationException()
    {
        // Arrange
        var request = MakeValidCreateRequest();
        request.StartTime = DateTime.UtcNow.AddDays(10);
        request.EndTime = DateTime.UtcNow.AddDays(5);

        _couponRepoMock
            .Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Coupon?)null);

        var svc = CreateService();

        // Act
        var act = () => svc.CreateCouponAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*End time must be after start time*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CreateCouponAsync")]
    public async Task CreateCouponAsync_WhenPercentOver100_ThrowsInvalidOperationException()
    {
        // Arrange
        var request = MakeValidCreateRequest();
        request.Type = "PERCENT";
        request.DiscountValue = 150;

        _couponRepoMock
            .Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Coupon?)null);

        var svc = CreateService();

        // Act
        var act = () => svc.CreateCouponAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*between 0 and 100*");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "CreateCouponAsync")]
    public async Task CreateCouponAsync_WhenPercentExactly100_Succeeds()
    {
        // Arrange
        var request = MakeValidCreateRequest();
        request.Type = "PERCENT";
        request.DiscountValue = 100;

        _couponRepoMock
            .Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Coupon?)null);
        _lookupResolverMock
            .Setup(r => r.GetIdAsync((ushort)LookupTypeEnum.CouponType, "PERCENT", It.IsAny<CancellationToken>()))
            .ReturnsAsync(PercentTypeId);
        _lookupResolverMock
            .Setup(r => r.GetIdAsync((ushort)LookupTypeEnum.CouponStatus, It.IsAny<Enum>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ActiveStatusId);
        _couponRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<Coupon>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Coupon c, CancellationToken _) =>
            {
                c.CouponId = 102;
                c.TypeLv = MakeTypeLv("PERCENT");
                c.CouponStatusLv = MakeStatusLv();
                return c;
            });

        var svc = CreateService();

        // Act
        var result = await svc.CreateCouponAsync(request, CancellationToken.None);

        // Assert
        result.DiscountValue.Should().Be(100);
        result.Type.Should().Be("PERCENT");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "CreateCouponAsync")]
    public async Task CreateCouponAsync_WhenCodeExactly3Chars_Succeeds()
    {
        // Arrange
        var request = MakeValidCreateRequest();
        request.CouponCode = "ABC";

        _couponRepoMock
            .Setup(r => r.GetByCodeAsync("ABC", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Coupon?)null);
        _lookupResolverMock
            .Setup(r => r.GetIdAsync((ushort)LookupTypeEnum.CouponType, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FixedAmountTypeId);
        _lookupResolverMock
            .Setup(r => r.GetIdAsync((ushort)LookupTypeEnum.CouponStatus, It.IsAny<Enum>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ActiveStatusId);
        _couponRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<Coupon>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Coupon c, CancellationToken _) =>
            {
                c.CouponId = 103;
                c.TypeLv = MakeTypeLv();
                c.CouponStatusLv = MakeStatusLv();
                return c;
            });

        var svc = CreateService();

        // Act
        var result = await svc.CreateCouponAsync(request, CancellationToken.None);

        // Assert
        result.CouponCode.Should().Be("ABC");
    }

    #endregion

    // ══════════════════════════════════════════════════════════════════
    //  UpdateCouponAsync
    // ══════════════════════════════════════════════════════════════════

    #region UpdateCouponAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "UpdateCouponAsync")]
    public async Task UpdateCouponAsync_WhenUnusedCoupon_PerformsFullUpdate()
    {
        // Arrange
        var coupon = MakeValidCoupon(id: 10, code: "OLD_CODE", usedCount: 0);
        var request = MakeValidUpdateRequest();

        _couponRepoMock
            .Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(coupon);
        _couponRepoMock
            .Setup(r => r.GetByCodeAsync("UPDATED2025", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Coupon?)null);
        _lookupResolverMock
            .Setup(r => r.GetIdAsync((ushort)LookupTypeEnum.CouponType, "FIXED_AMOUNT", It.IsAny<CancellationToken>()))
            .ReturnsAsync(FixedAmountTypeId);
        _couponRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<Coupon>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Coupon c, CancellationToken _) =>
            {
                c.TypeLv = MakeTypeLv();
                c.CouponStatusLv = MakeStatusLv();
                return c;
            });

        var svc = CreateService();

        // Act
        var result = await svc.UpdateCouponAsync(10, request, CancellationToken.None);

        // Assert
        result.CouponCode.Should().Be("UPDATED2025");
        result.CouponName.Should().Be("Updated Coupon");
        result.DiscountValue.Should().Be(30000);
        _couponRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Coupon>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "UpdateCouponAsync")]
    public async Task UpdateCouponAsync_WhenUsedCoupon_OnlyUpdatesAllowedFields()
    {
        // Arrange
        var coupon = MakeValidCoupon(id: 11, code: "USED_CODE", usedCount: 5);
        var request = MakeValidUpdateRequest();
        request.Description = "Updated description only";
        request.EndTime = DateTime.UtcNow.AddDays(90);
        request.MaxUsage = 500;

        _couponRepoMock
            .Setup(r => r.GetByIdAsync(11, It.IsAny<CancellationToken>()))
            .ReturnsAsync(coupon);
        _couponRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<Coupon>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Coupon c, CancellationToken _) =>
            {
                c.TypeLv = MakeTypeLv();
                c.CouponStatusLv = MakeStatusLv();
                return c;
            });

        var svc = CreateService();

        // Act
        var result = await svc.UpdateCouponAsync(11, request, CancellationToken.None);

        // Assert
        result.CouponCode.Should().Be("USED_CODE"); // Code should NOT change
        result.MaxUsage.Should().Be(500);
        _couponRepoMock.Verify(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "UpdateCouponAsync")]
    public async Task UpdateCouponAsync_WhenCouponNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _couponRepoMock
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Coupon?)null);

        var svc = CreateService();

        // Act
        var act = () => svc.UpdateCouponAsync(999, MakeValidUpdateRequest(), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*999*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "UpdateCouponAsync")]
    public async Task UpdateCouponAsync_WhenCouponExpired_ThrowsInvalidOperationException()
    {
        // Arrange
        var coupon = MakeValidCoupon(
            id: 12,
            end: DateTime.UtcNow.AddDays(-1));

        _couponRepoMock
            .Setup(r => r.GetByIdAsync(12, It.IsAny<CancellationToken>()))
            .ReturnsAsync(coupon);

        var svc = CreateService();

        // Act
        var act = () => svc.UpdateCouponAsync(12, MakeValidUpdateRequest(), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*expired*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "UpdateCouponAsync")]
    public async Task UpdateCouponAsync_WhenUnusedAndDuplicateCode_ThrowsInvalidOperationException()
    {
        // Arrange
        var coupon = MakeValidCoupon(id: 13, code: "ORIGINAL", usedCount: 0);
        var request = MakeValidUpdateRequest();
        request.CouponCode = "DUPLICATE";

        _couponRepoMock
            .Setup(r => r.GetByIdAsync(13, It.IsAny<CancellationToken>()))
            .ReturnsAsync(coupon);
        _couponRepoMock
            .Setup(r => r.GetByCodeAsync("DUPLICATE", It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeValidCoupon(id: 99, code: "DUPLICATE"));

        var svc = CreateService();

        // Act
        var act = () => svc.UpdateCouponAsync(13, request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "UpdateCouponAsync")]
    public async Task UpdateCouponAsync_WhenUnusedAndEndBeforeStart_ThrowsInvalidOperationException()
    {
        // Arrange
        var coupon = MakeValidCoupon(id: 14, usedCount: 0);
        var request = MakeValidUpdateRequest();
        request.StartTime = DateTime.UtcNow.AddDays(10);
        request.EndTime = DateTime.UtcNow.AddDays(5);

        _couponRepoMock
            .Setup(r => r.GetByIdAsync(14, It.IsAny<CancellationToken>()))
            .ReturnsAsync(coupon);
        _couponRepoMock
            .Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Coupon?)null);

        var svc = CreateService();

        // Act
        var act = () => svc.UpdateCouponAsync(14, request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*End time must be after start time*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "UpdateCouponAsync")]
    public async Task UpdateCouponAsync_WhenUsedAndEndBeforeStart_ThrowsInvalidOperationException()
    {
        // Arrange
        var coupon = MakeValidCoupon(id: 15, usedCount: 3);
        var request = MakeValidUpdateRequest();
        request.EndTime = coupon.StartTime.AddDays(-1);

        _couponRepoMock
            .Setup(r => r.GetByIdAsync(15, It.IsAny<CancellationToken>()))
            .ReturnsAsync(coupon);

        var svc = CreateService();

        // Act
        var act = () => svc.UpdateCouponAsync(15, request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*End time must be after start time*");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "UpdateCouponAsync")]
    public async Task UpdateCouponAsync_WhenUnusedAndSameCode_SkipsUniquenessCheck()
    {
        // Arrange
        var coupon = MakeValidCoupon(id: 16, code: "SAMECODE", usedCount: 0);
        var request = MakeValidUpdateRequest();
        request.CouponCode = "SAMECODE";

        _couponRepoMock
            .Setup(r => r.GetByIdAsync(16, It.IsAny<CancellationToken>()))
            .ReturnsAsync(coupon);
        _lookupResolverMock
            .Setup(r => r.GetIdAsync((ushort)LookupTypeEnum.CouponType, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FixedAmountTypeId);
        _couponRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<Coupon>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Coupon c, CancellationToken _) =>
            {
                c.TypeLv = MakeTypeLv();
                c.CouponStatusLv = MakeStatusLv();
                return c;
            });

        var svc = CreateService();

        // Act
        var result = await svc.UpdateCouponAsync(16, request, CancellationToken.None);

        // Assert
        result.CouponCode.Should().Be("SAMECODE");
        _couponRepoMock.Verify(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    // ══════════════════════════════════════════════════════════════════
    //  DeleteCouponAsync
    // ══════════════════════════════════════════════════════════════════

    #region DeleteCouponAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "DeleteCouponAsync")]
    public async Task DeleteCouponAsync_WhenUnusedCoupon_DeletesSuccessfully()
    {
        // Arrange
        var coupon = MakeValidCoupon(id: 20, usedCount: 0);
        _couponRepoMock
            .Setup(r => r.GetByIdAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(coupon);
        _couponRepoMock
            .Setup(r => r.DeleteAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var svc = CreateService();

        // Act
        await svc.DeleteCouponAsync(20, CancellationToken.None);

        // Assert
        _couponRepoMock.Verify(r => r.DeleteAsync(20, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "DeleteCouponAsync")]
    public async Task DeleteCouponAsync_WhenCouponNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _couponRepoMock
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Coupon?)null);

        var svc = CreateService();

        // Act
        var act = () => svc.DeleteCouponAsync(999, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*999*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "DeleteCouponAsync")]
    public async Task DeleteCouponAsync_WhenCouponUsed_ThrowsInvalidOperationException()
    {
        // Arrange
        var coupon = MakeValidCoupon(id: 21, usedCount: 3);
        _couponRepoMock
            .Setup(r => r.GetByIdAsync(21, It.IsAny<CancellationToken>()))
            .ReturnsAsync(coupon);

        var svc = CreateService();

        // Act
        var act = () => svc.DeleteCouponAsync(21, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*has been used*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "DeleteCouponAsync")]
    public async Task DeleteCouponAsync_WhenRepoReturnsFalse_ThrowsKeyNotFoundException()
    {
        // Arrange
        var coupon = MakeValidCoupon(id: 22, usedCount: 0);
        _couponRepoMock
            .Setup(r => r.GetByIdAsync(22, It.IsAny<CancellationToken>()))
            .ReturnsAsync(coupon);
        _couponRepoMock
            .Setup(r => r.DeleteAsync(22, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var svc = CreateService();

        // Act
        var act = () => svc.DeleteCouponAsync(22, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Failed to delete*");
    }

    #endregion
}
