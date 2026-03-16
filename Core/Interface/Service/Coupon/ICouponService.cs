using Core.DTO.Coupon;

namespace Core.Interface.Service.Coupon
{
    public interface ICouponService
    {
        Task<List<CouponDTO>> GetCouponsAsync(CancellationToken ct);
    }
}
