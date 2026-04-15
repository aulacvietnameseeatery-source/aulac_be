using Core.DTO.Coupon;
using Core.Entity;
using Core.Interface.Repo;
using Core.Interface.Service.Entity;
using Core.Service;
using FluentAssertions;
using Moq;

namespace Tests.Services;

/// <summary>
/// Unit Test - CouponService
/// Code Module : Core/Service/CouponService.cs
/// Method      : GetCouponsAsync, GetCouponDetailAsync, CreateCouponAsync, UpdateCouponAsync, DeleteCouponAsync
/// Created By  : quantm
/// Executed By : quantm
/// Test Req.   : Test business rules for coupon filtering, create/update validation, and delete constraints.
/// </summary>
public class CouponServiceTests
{
    // Mocks
    private readonly Mock<ICouponRepository> _couponRepositoryMock = new();
    private readonly Mock<ILookupResolver> _lookupResolverMock = new();

    private const uint CouponTypePercentId = 11;
    private const uint CouponStatusActiveId = 21;

    private CouponService CreateService() => new(
        _couponRepositoryMock.Object,
        _lookupResolverMock.Object);

    // Helper: tạo Coupon giả
    private static Coupon MakeCoupon(
        long id = 1,
        string code = "SAVE10",
        string name = "Save 10",
        long? customerId = null,
        int? usedCount = 0,
        string typeCode = "PERCENT",
        string statusCode = "ACTIVE") => new()
    {
        CouponId = id,
        CouponCode = code,
        CouponName = name,
        CustomerId = customerId,
        StartTime = DateTime.UtcNow.AddDays(-1),
        EndTime = DateTime.UtcNow.AddDays(10),
        DiscountValue = 10,
        MaxUsage = 100,
        UsedCount = usedCount,
        TypeLv = new LookupValue { ValueCode = typeCode },
        CouponStatusLv = new LookupValue { ValueCode = statusCode },
        Customer = customerId.HasValue ? new Customer { FullName = "Customer " + customerId } : null
    };

    // Helper: tạo CreateCouponRequest giả
    private static CreateCouponRequest MakeCreateRequest() => new()
    {
        CouponCode = " save10 ",
        CouponName = "  Save 10  ",
        Description = "  desc  ",
        StartTime = DateTime.UtcNow.AddDays(1),
        EndTime = DateTime.UtcNow.AddDays(2),
        DiscountValue = 10,
        MaxUsage = 100,
        Type = "PERCENT"
    };

    // Helper: tạo UpdateCouponRequest giả
    private static UpdateCouponRequest MakeUpdateRequest() => new()
    {
        CouponCode = " up20 ",
        CouponName = "  Update 20  ",
        Description = "  new desc  ",
        StartTime = DateTime.UtcNow.AddDays(3),
        EndTime = DateTime.UtcNow.AddDays(4),
        DiscountValue = 20,
        MaxUsage = 50,
        Type = "PERCENT"
    };

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetCouponsAsync")]
    public async Task GetCouponsAsync_WhenNoCustomerId_ReturnsAllActiveCoupons()
    {
        var coupons = new List<Coupon>
        {
            MakeCoupon(id: 1, customerId: null),
            MakeCoupon(id: 2, customerId: 100)
        };

        _couponRepositoryMock
            .Setup(r => r.GetActiveCouponsAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(coupons);

        var service = CreateService();

        var result = await service.GetCouponsAsync(null, CancellationToken.None);

        result.Should().HaveCount(2);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetCouponsAsync")]
    public async Task GetCouponsAsync_WhenCustomerIdProvided_FiltersByCustomerIdOrGlobal()
    {
        var coupons = new List<Coupon>
        {
            MakeCoupon(id: 1, customerId: null),
            MakeCoupon(id: 2, customerId: 999),
            MakeCoupon(id: 3, customerId: 100)
        };

        _couponRepositoryMock
            .Setup(r => r.GetActiveCouponsAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(coupons);

        var service = CreateService();

        var result = await service.GetCouponsAsync(100, CancellationToken.None);

        result.Should().HaveCount(2);
        result.Select(x => x.CouponId).Should().BeEquivalentTo([1, 3]);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetCouponDetailAsync")]
    public async Task GetCouponDetailAsync_WhenCouponExists_ReturnsDetail()
    {
        var coupon = MakeCoupon(id: 5, code: "C5", name: "Coupon 5");

        _couponRepositoryMock
            .Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(coupon);

        var service = CreateService();

        var result = await service.GetCouponDetailAsync(5, CancellationToken.None);

        result.CouponId.Should().Be(5);
        result.CouponCode.Should().Be("C5");
        result.Type.Should().Be("PERCENT");
        result.CouponStatus.Should().Be("ACTIVE");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "GetCouponDetailAsync")]
    public async Task GetCouponDetailAsync_WhenCouponNotFound_ThrowsKeyNotFoundException()
    {
        _couponRepositoryMock
            .Setup(r => r.GetByIdAsync(404, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Coupon?)null);

        var service = CreateService();

        await service.Invoking(s => s.GetCouponDetailAsync(404, CancellationToken.None))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "CreateCouponAsync")]
    public async Task CreateCouponAsync_WhenValidRequest_CreatesCouponWithNormalizedCode()
    {
        var request = MakeCreateRequest();

        _couponRepositoryMock
            .Setup(r => r.GetByCodeAsync("SAVE10", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Coupon?)null);

        _lookupResolverMock
            .Setup(r => r.GetIdAsync(It.IsAny<ushort>(), "PERCENT", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CouponTypePercentId);

        _lookupResolverMock
            .Setup(r => r.GetIdAsync(It.IsAny<ushort>(), "ACTIVE", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CouponStatusActiveId);

        _couponRepositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<Coupon>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Coupon c, CancellationToken _) =>
            {
                c.CouponId = 10;
                c.TypeLv = new LookupValue { ValueCode = "PERCENT" };
                c.CouponStatusLv = new LookupValue { ValueCode = "ACTIVE" };
                return c;
            });

        var service = CreateService();

        var result = await service.CreateCouponAsync(request, CancellationToken.None);

        result.CouponId.Should().Be(10);
        result.CouponCode.Should().Be("SAVE10");
        result.CouponName.Should().Be("Save 10");
        result.Type.Should().Be("PERCENT");
        result.CouponStatus.Should().Be("ACTIVE");

        _couponRepositoryMock.Verify(
            r => r.CreateAsync(It.Is<Coupon>(c =>
                c.CouponCode == "SAVE10" &&
                c.CouponName == "Save 10" &&
                c.Description == "desc" &&
                c.TypeLvId == CouponTypePercentId &&
                c.CouponStatusLvId == CouponStatusActiveId), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CreateCouponAsync")]
    public async Task CreateCouponAsync_WhenCodeExists_ThrowsInvalidOperationException()
    {
        var request = MakeCreateRequest();

        _couponRepositoryMock
            .Setup(r => r.GetByCodeAsync("SAVE10", It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeCoupon(code: "SAVE10"));

        var service = CreateService();

        await service.Invoking(s => s.CreateCouponAsync(request, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CreateCouponAsync")]
    public async Task CreateCouponAsync_WhenEndTimeBeforeStartTime_ThrowsInvalidOperationException()
    {
        var request = MakeCreateRequest();
        request.StartTime = DateTime.UtcNow.AddDays(5);
        request.EndTime = DateTime.UtcNow.AddDays(4);

        _couponRepositoryMock
            .Setup(r => r.GetByCodeAsync("SAVE10", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Coupon?)null);

        var service = CreateService();

        await service.Invoking(s => s.CreateCouponAsync(request, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*End time must be after start time*");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "CreateCouponAsync")]
    public async Task CreateCouponAsync_WhenPercentIsExactly100_AllowsCreation()
    {
        var request = MakeCreateRequest();
        request.DiscountValue = 100;

        _couponRepositoryMock
            .Setup(r => r.GetByCodeAsync("SAVE10", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Coupon?)null);

        _lookupResolverMock
            .Setup(r => r.GetIdAsync(It.IsAny<ushort>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1u);

        _couponRepositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<Coupon>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Coupon c, CancellationToken _) =>
            {
                c.CouponId = 11;
                c.TypeLv = new LookupValue { ValueCode = "PERCENT" };
                c.CouponStatusLv = new LookupValue { ValueCode = "ACTIVE" };
                return c;
            });

        var service = CreateService();

        var result = await service.CreateCouponAsync(request, CancellationToken.None);

        result.DiscountValue.Should().Be(100);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "UpdateCouponAsync")]
    public async Task UpdateCouponAsync_WhenCouponNotFound_ThrowsKeyNotFoundException()
    {
        _couponRepositoryMock
            .Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Coupon?)null);

        var service = CreateService();

        await service.Invoking(s => s.UpdateCouponAsync(99, MakeUpdateRequest(), CancellationToken.None))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "UpdateCouponAsync")]
    public async Task UpdateCouponAsync_WhenCodeChangedAndUnique_UpdatesSuccessfully()
    {
        var existing = MakeCoupon(id: 8, code: "OLD10", name: "Old");
        var request = MakeUpdateRequest();

        _couponRepositoryMock
            .Setup(r => r.GetByIdAsync(8, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        _couponRepositoryMock
            .Setup(r => r.GetByCodeAsync("UP20", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Coupon?)null);

        _lookupResolverMock
            .Setup(r => r.GetIdAsync(It.IsAny<ushort>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1u);

        _couponRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Coupon>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Coupon c, CancellationToken _) =>
            {
                c.TypeLv = new LookupValue { ValueCode = "PERCENT" };
                c.CouponStatusLv = new LookupValue { ValueCode = "ACTIVE" };
                return c;
            });

        var service = CreateService();

        var result = await service.UpdateCouponAsync(8, request, CancellationToken.None);

        result.CouponCode.Should().Be("UP20");
        result.CouponName.Should().Be("Update 20");
        result.DiscountValue.Should().Be(20);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "UpdateCouponAsync")]
    public async Task UpdateCouponAsync_WhenPercentGreaterThan100_ThrowsInvalidOperationException()
    {
        var existing = MakeCoupon(id: 8, code: "OLD10", name: "Old");
        var request = MakeUpdateRequest();
        request.DiscountValue = 100.01m;

        _couponRepositoryMock
            .Setup(r => r.GetByIdAsync(8, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        _couponRepositoryMock
            .Setup(r => r.GetByCodeAsync("UP20", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Coupon?)null);

        var service = CreateService();

        await service.Invoking(s => s.UpdateCouponAsync(8, request, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*between 0 and 100%*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "DeleteCouponAsync")]
    public async Task DeleteCouponAsync_WhenCouponUsed_ThrowsInvalidOperationException()
    {
        _couponRepositoryMock
            .Setup(r => r.GetByIdAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeCoupon(id: 20, usedCount: 1));

        var service = CreateService();

        await service.Invoking(s => s.DeleteCouponAsync(20, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*has been used*");
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "DeleteCouponAsync")]
    public async Task DeleteCouponAsync_WhenValid_DeletesSuccessfully()
    {
        _couponRepositoryMock
            .Setup(r => r.GetByIdAsync(30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeCoupon(id: 30, usedCount: 0));

        _couponRepositoryMock
            .Setup(r => r.DeleteAsync(30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = CreateService();

        await service.DeleteCouponAsync(30, CancellationToken.None);

        _couponRepositoryMock.Verify(r => r.DeleteAsync(30, It.IsAny<CancellationToken>()), Times.Once);
    }
}
