using Core.DTO.Coupon;
using Core.Interface.Repo;
using Core.Interface.Service.Coupon;

namespace Core.Service
{
    public class CouponService : ICouponService
    {
        private readonly ICouponRepository _couponRepository;

        public CouponService(ICouponRepository couponRepository)
        {
            _couponRepository = couponRepository;
        }

        public async Task<List<CouponDTO>> GetCouponsAsync(CancellationToken ct)
        {
            var now = DateTime.Now;
            var coupons = await _couponRepository.GetActiveCouponsAsync(now, ct);

            return coupons
                .Select(c => new CouponDTO
                {
                    CouponId = c.CouponId,
                    CouponCode = c.CouponCode,
                    CouponName = c.CouponName,
                    StartTime = c.StartTime,
                    EndTime = c.EndTime,
                    DiscountValue = c.DiscountValue,
                    MaxUsage = c.MaxUsage,
                    UsedCount = c.UsedCount,
                    Type = c.TypeLv.ValueCode,
                    CouponStatus = c.CouponStatusLv.ValueCode
                })
                .ToList();
        }
    }
}
