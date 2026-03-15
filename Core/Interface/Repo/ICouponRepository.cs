using Core.Entity;

namespace Core.Interface.Repo
{
    public interface ICouponRepository
    {
        Task<List<Coupon>> GetActiveCouponsAsync(DateTime now, CancellationToken ct);
    }
}
