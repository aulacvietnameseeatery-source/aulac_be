using Core.DTO.Coupon;
using Core.DTO.General;
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
                .Include(c => c.Customer)
                .Include(c => c.TypeLv)
                .Include(c => c.CouponStatusLv)
                .Where(c =>
                    !string.IsNullOrWhiteSpace(c.CouponCode) &&
                    c.StartTime <= now &&
                    c.EndTime >= now &&
                    (c.CouponStatusLv.ValueCode == "ACTIVE" || c.CouponStatusLv.ValueCode == "SCHEDULED") &&
                    (c.MaxUsage == null || (c.UsedCount ?? 0) < c.MaxUsage))
                .OrderBy(c => c.CouponCode)
                .ToListAsync(ct);
        }

        public async Task<PagedResultDTO<CouponDTO>> GetAllCouponsAsync(CouponListQueryDTO query, CancellationToken ct)
        {
            var queryable = _context.Coupons
                .AsNoTracking()
                .Include(c => c.TypeLv)
                .Include(c => c.CouponStatusLv)
                .AsQueryable();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var searchLower = query.Search.ToLower();
                queryable = queryable.Where(c =>
                    c.CouponCode.ToLower().Contains(searchLower) ||
                    c.CouponName.ToLower().Contains(searchLower));
            }

            // Get total count
            var totalCount = await queryable.CountAsync(ct);

            // Apply pagination and fetch data
            var coupons = await queryable
                .OrderByDescending(c => c.CouponId)
                .Skip((query.PageIndex - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(c => new CouponDTO
                {
                    CouponId = c.CouponId,
                    CouponCode = c.CouponCode,
                    CouponName = c.CouponName,
                    CustomerId = c.CustomerId,
                    CustomerName = c.Customer != null ? c.Customer.FullName : null,
                    StartTime = c.StartTime,
                    EndTime = c.EndTime,
                    DiscountValue = c.DiscountValue,
                    MaxUsage = c.MaxUsage,
                    UsedCount = c.UsedCount,
                    Type = c.TypeLv.ValueCode,
                    CouponStatus = c.CouponStatusLv.ValueCode
                })
                .ToListAsync(ct);

            return new PagedResultDTO<CouponDTO>
            {
                PageData = coupons,
                PageIndex = query.PageIndex,
                PageSize = query.PageSize,
                TotalCount = totalCount
            };
        }

        public async Task<Coupon?> GetByIdAsync(long id, CancellationToken ct)
        {
            return await _context.Coupons
                .Include(c => c.TypeLv)
                .Include(c => c.CouponStatusLv)
                .FirstOrDefaultAsync(c => c.CouponId == id, ct);
        }

        public async Task<Coupon?> GetByCodeAsync(string code, CancellationToken ct)
        {
            return await _context.Coupons
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CouponCode.ToLower() == code.ToLower(), ct);
        }

        public async Task<Coupon> CreateAsync(Coupon coupon, CancellationToken ct)
        {
            _context.Coupons.Add(coupon);
            await _context.SaveChangesAsync(ct);
            
            // Reload with navigation properties
            await _context.Entry(coupon)
                .Reference(c => c.TypeLv)
                .LoadAsync(ct);
            await _context.Entry(coupon)
                .Reference(c => c.CouponStatusLv)
                .LoadAsync(ct);
            
            return coupon;
        }

        public async Task<Coupon> UpdateAsync(Coupon coupon, CancellationToken ct)
        {
            _context.Coupons.Update(coupon);
            await _context.SaveChangesAsync(ct);
            
            // Reload with navigation properties
            await _context.Entry(coupon)
                .Reference(c => c.TypeLv)
                .LoadAsync(ct);
            await _context.Entry(coupon)
                .Reference(c => c.CouponStatusLv)
                .LoadAsync(ct);
            
            return coupon;
        }

        public async Task<bool> DeleteAsync(long id, CancellationToken ct)
        {
            var coupon = await _context.Coupons.FindAsync(new object[] { id }, ct);
            if (coupon == null)
                return false;

            _context.Coupons.Remove(coupon);
            await _context.SaveChangesAsync(ct);
            return true;
        }
    }
}
