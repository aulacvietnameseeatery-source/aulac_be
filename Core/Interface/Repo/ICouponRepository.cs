using Core.DTO.Coupon;
using Core.DTO.General;
using Core.Entity;

namespace Core.Interface.Repo
{
    public interface ICouponRepository
    {
        Task<List<Coupon>> GetActiveCouponsAsync(DateTime now, CancellationToken ct);
        Task<PagedResultDTO<CouponDTO>> GetAllCouponsAsync(CouponListQueryDTO query, CancellationToken ct);
        Task<Coupon?> GetByIdAsync(long id, CancellationToken ct);
        Task<Coupon?> GetByCodeAsync(string code, CancellationToken ct);
        Task<Coupon> CreateAsync(Coupon coupon, CancellationToken ct);
        Task<Coupon> UpdateAsync(Coupon coupon, CancellationToken ct);
        Task<bool> DeleteAsync(long id, CancellationToken ct);
    }
}
