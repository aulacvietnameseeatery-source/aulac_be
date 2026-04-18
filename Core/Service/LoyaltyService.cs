using Core.DTO.Loyalty;
using Core.Entity;
using Core.Enum;
using Core.Extensions;
using Core.Interface.Repo;
using Core.Interface.Service.Entity;
using Core.Interface.Service.Loyalty;
using LookupTypeEnum = Core.Enum.LookupType;

namespace Infa.Service;

public class LoyaltyService : ILoyaltyService
{
    private const string RedemptionEnabledSettingKey = "loyalty.redemption_enabled";
    private const string RedemptionRatioSettingKey = "loyalty.points_to_currency_ratio";
    private const string RedemptionMinPointsSettingKey = "loyalty.min_redemption_points";
    private const int CouponCodeLength = 8;
    private const int CouponValidityDays = 30;

    private readonly ILoyaltyRepository _loyaltyRepository;
    private readonly ILookupResolver _lookupResolver;
    private readonly ISystemSettingService _systemSettingService;
    private readonly IUnitOfWork _unitOfWork;

    public LoyaltyService(
        ILoyaltyRepository loyaltyRepository,
        ILookupResolver lookupResolver,
        ISystemSettingService systemSettingService,
        IUnitOfWork unitOfWork)
    {
        _loyaltyRepository = loyaltyRepository;
        _lookupResolver = lookupResolver;
        _systemSettingService = systemSettingService;
        _unitOfWork = unitOfWork;
    }

    public async Task<LoyaltyExchangeResultDto> ExchangePointsToCouponAsync(LoyaltyExchangeRequest request, CancellationToken ct = default)
    {
        if (request.Points <= 0)
        {
            throw new InvalidOperationException("Points must be greater than 0.");
        }

        var redemptionEnabled = await _systemSettingService.GetBoolAsync(RedemptionEnabledSettingKey, false, ct) ?? false;
        if (!redemptionEnabled)
        {
            throw new InvalidOperationException("Loyalty redemption is currently disabled.");
        }

        var pointsToCurrencyRatio = await _systemSettingService.GetDecimalAsync(RedemptionRatioSettingKey, null, ct)
            ?? throw new InvalidOperationException("loyalty.points_to_currency_ratio is missing or invalid.");

        var minRedemptionPoints = await _systemSettingService.GetIntAsync(RedemptionMinPointsSettingKey, null, ct)
            ?? throw new InvalidOperationException("loyalty.min_redemption_points is missing or invalid.");

        if (request.Points < minRedemptionPoints)
        {
            throw new InvalidOperationException($"Minimum redemption is {minRedemptionPoints} points.");
        }

        var customer = await _loyaltyRepository.GetCustomerByIdAsync(request.CustomerId, ct)
            ?? throw new KeyNotFoundException($"Customer {request.CustomerId} not found.");

        var currentPoints = customer.LoyaltyPoints ?? 0;
        if (currentPoints < request.Points)
        {
            throw new InvalidOperationException("Customer does not have enough loyalty points.");
        }

        var couponTypeId = await CouponTypeCode.FIXED_AMOUNT.IdAsync(_lookupResolver, (ushort)LookupTypeEnum.CouponType, ct);
        var couponStatusId = await CouponStatusCode.ACTIVE.IdAsync(_lookupResolver, (ushort)LookupTypeEnum.CouponStatus, ct);

        var discountValue = request.Points * pointsToCurrencyRatio;
        if (discountValue <= 0)
        {
            throw new InvalidOperationException("Calculated coupon value must be greater than 0.");
        }

        var now = DateTime.UtcNow;
        var coupon = new Coupon
        {
            CustomerId = customer.CustomerId,
            CouponCode = await GenerateUniqueCouponCodeAsync(ct),
            CouponName = $"Loyalty Redemption {request.Points} pts",
            Description = $"Redeemed from {request.Points} loyalty points.",
            StartTime = now,
            EndTime = now.AddDays(CouponValidityDays),
            DiscountValue = discountValue,
            MaxUsage = 1,
            UsedCount = 0,
            TypeLvId = couponTypeId,
            CouponStatusLvId = couponStatusId,
            CreatedAt = now
        };

        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            customer.LoyaltyPoints = currentPoints - request.Points;
            await _loyaltyRepository.AddCouponAsync(coupon, ct);

            await _unitOfWork.SaveChangesAsync(ct);
            await _unitOfWork.CommitAsync(ct);

            return new LoyaltyExchangeResultDto
            {
                CustomerId = customer.CustomerId,
                SpentPoints = request.Points,
                RemainingPoints = customer.LoyaltyPoints ?? 0,
                CouponId = coupon.CouponId,
                CouponCode = coupon.CouponCode,
                CouponName = coupon.CouponName,
                DiscountValue = coupon.DiscountValue,
                StartTime = coupon.StartTime,
                EndTime = coupon.EndTime
            };
        }
        catch
        {
            await _unitOfWork.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<List<LoyaltyCouponHistoryDto>> GetCustomerCouponsAsync(long customerId, CancellationToken ct = default)
    {
        var customerExists = await _loyaltyRepository.CustomerExistsAsync(customerId, ct);
        if (!customerExists)
        {
            throw new KeyNotFoundException($"Customer {customerId} not found.");
        }

        var now = DateTime.UtcNow;
        var coupons = await _loyaltyRepository.GetCustomerCouponsAsync(customerId, ct);

        return coupons.Select(c => new LoyaltyCouponHistoryDto
        {
            CouponId = c.CouponId,
            CustomerId = c.CustomerId,
            CouponCode = c.CouponCode,
            CouponName = c.CouponName,
            DiscountValue = c.DiscountValue,
            StartTime = c.StartTime,
            EndTime = c.EndTime,
            MaxUsage = c.MaxUsage,
            UsedCount = c.UsedCount,
            CouponStatus = c.CouponStatusLv.ValueCode,
            IsExpired = now > c.EndTime,
            IsUsed = (c.UsedCount ?? 0) >= (c.MaxUsage ?? int.MaxValue),
            RedemptionStatus = ResolveRedemptionStatus(c, now)
        }).ToList();
    }

    private async Task<string> GenerateUniqueCouponCodeAsync(CancellationToken ct)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var code = $"LP-{GenerateRandomCode(CouponCodeLength)}";
            var exists = await _loyaltyRepository.CouponCodeExistsAsync(code, ct);
            if (!exists)
            {
                return code;
            }
        }

        throw new InvalidOperationException("Failed to generate a unique loyalty coupon code.");
    }

    private static string GenerateRandomCode(int length)
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var buffer = new char[length];

        for (var i = 0; i < length; i++)
        {
            var index = Random.Shared.Next(alphabet.Length);
            buffer[i] = alphabet[index];
        }

        return new string(buffer);
    }

    private static string ResolveRedemptionStatus(Coupon coupon, DateTime now)
    {
        if (string.Equals(coupon.CouponStatusLv?.ValueCode, CouponStatusCode.DISABLED.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            return "DISABLED";
        }

        if (now > coupon.EndTime)
        {
            return "EXPIRED";
        }

        if (coupon.MaxUsage.HasValue && (coupon.UsedCount ?? 0) >= coupon.MaxUsage.Value)
        {
            return "USED";
        }

        return "UNUSED";
    }
}