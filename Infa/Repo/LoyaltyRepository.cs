using Core.Entity;
using Core.Interface.Repo;
using Infa.Data;
using Microsoft.EntityFrameworkCore;

namespace Infa.Repo;

public class LoyaltyRepository : ILoyaltyRepository
{
    private readonly RestaurantMgmtContext _context;

    public LoyaltyRepository(RestaurantMgmtContext context)
    {
        _context = context;
    }

    public Task<Customer?> GetCustomerByIdAsync(long customerId, CancellationToken ct)
    {
        return _context.Customers
            .FirstOrDefaultAsync(c => c.CustomerId == customerId, ct);
    }

    public Task<bool> CustomerExistsAsync(long customerId, CancellationToken ct)
    {
        return _context.Customers
            .AnyAsync(c => c.CustomerId == customerId, ct);
    }

    public Task AddCouponAsync(Coupon coupon, CancellationToken ct)
    {
        return _context.Coupons.AddAsync(coupon, ct).AsTask();
    }

    public Task<bool> CouponCodeExistsAsync(string couponCode, CancellationToken ct)
    {
        return _context.Coupons
            .AnyAsync(c => c.CouponCode == couponCode, ct);
    }

    public Task<List<Coupon>> GetCustomerCouponsAsync(long customerId, CancellationToken ct)
    {
        return _context.Coupons
            .AsNoTracking()
            .Include(c => c.CouponStatusLv)
            .Where(c => c.CustomerId == customerId)
            .OrderByDescending(c => c.CreatedAt ?? c.StartTime)
            .ThenByDescending(c => c.CouponId)
            .ToListAsync(ct);
    }
}
