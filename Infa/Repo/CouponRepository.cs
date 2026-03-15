using Core.Entity;
using Core.Interface.Repo;
using Infa.Data;
using Microsoft.EntityFrameworkCore;

namespace Infa.Repo
{
    public class CouponRepository : ICouponRepository
    {
        private readonly RestaurantMgmtContext _context;

        public CouponRepository(RestaurantMgmtContext context)
        {
            _context = context;
        }

        public async Task<List<Coupon>> GetActiveCouponsAsync(DateTime now, CancellationToken ct)
        {
            return await _context.Coupons
                .AsNoTracking()
                .Include(c => c.TypeLv)
                .Include(c => c.CouponStatusLv)
                .Where(c =>
                    !string.IsNullOrWhiteSpace(c.CouponCode) &&
                    c.StartTime <= now &&
                    c.EndTime >= now &&
                    c.CouponStatusLv.ValueCode == "ACTIVE" &&
                    (c.MaxUsage == null || (c.UsedCount ?? 0) < c.MaxUsage))
                .OrderBy(c => c.CouponCode)
                .ToListAsync(ct);
        }
    }
}
