using Core.Entity;

namespace Core.Interface.Repo;

public interface ILoyaltyRepository
{
    Task<Customer?> GetCustomerByIdAsync(long customerId, CancellationToken ct);
    Task<bool> CustomerExistsAsync(long customerId, CancellationToken ct);
    Task AddCouponAsync(Coupon coupon, CancellationToken ct);
    Task<bool> CouponCodeExistsAsync(string couponCode, CancellationToken ct);
    Task<List<Coupon>> GetCustomerCouponsAsync(long customerId, CancellationToken ct);
}
