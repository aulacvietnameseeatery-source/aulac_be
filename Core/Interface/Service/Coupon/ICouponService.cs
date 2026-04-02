using Core.DTO.Coupon;
using Core.DTO.General;

namespace Core.Interface.Service.Coupon
{
    public interface ICouponService
    {
        Task<List<CouponDTO>> GetCouponsAsync(long? customerId, CancellationToken ct);
        Task<PagedResultDTO<CouponDTO>> GetAllCouponsAsync(CouponListQueryDTO query, CancellationToken ct);
        Task<CouponDetailDto> GetCouponDetailAsync(long id, CancellationToken ct);
        Task<CouponDTO> CreateCouponAsync(CreateCouponRequest request, CancellationToken ct);
        Task<CouponDTO> UpdateCouponAsync(long id, UpdateCouponRequest request, CancellationToken ct);
        Task DeleteCouponAsync(long id, CancellationToken ct);
    }
}
