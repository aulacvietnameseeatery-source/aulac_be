using Core.DTO.Coupon;
using Core.DTO.General;
using Core.Entity;
using Core.Enum;
using Core.Interface.Repo;
using Core.Interface.Service.Coupon;
using Core.Interface.Service.Entity;
using LookupTypeEnum = Core.Enum.LookupType;

namespace Core.Service
{
    public class CouponService : ICouponService
    {
        private readonly ICouponRepository _couponRepository;
        private readonly ILookupResolver _lookupResolver;

        public CouponService(
            ICouponRepository couponRepository,
            ILookupResolver lookupResolver)
        {
            _couponRepository = couponRepository;
            _lookupResolver = lookupResolver;
        }

        public async Task<List<CouponDTO>> GetCouponsAsync(long? customerId, CancellationToken ct)
        {
            var now = DateTime.Now;
            var coupons = await _couponRepository.GetActiveCouponsAsync(now, ct);

            if (customerId.HasValue)
            {
                coupons = coupons
                    .Where(c => !c.CustomerId.HasValue || c.CustomerId == customerId.Value)
                    .ToList();
            }

            return coupons
                .Select(c => new CouponDTO
                {
                    CouponId = c.CouponId,
                    CouponCode = c.CouponCode,
                    CouponName = c.CouponName,
                    CustomerId = c.CustomerId,
                    CustomerName = c.Customer?.FullName,
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

        public async Task<PagedResultDTO<CouponDTO>> GetAllCouponsAsync(CouponListQueryDTO query, CancellationToken ct)
        {
            return await _couponRepository.GetAllCouponsAsync(query, ct);
        }

        public async Task<CouponDetailDto> GetCouponDetailAsync(long id, CancellationToken ct)
        {
            var coupon = await _couponRepository.GetByIdAsync(id, ct);
            
            if (coupon == null)
                throw new KeyNotFoundException($"Coupon with ID {id} not found.");

            return new CouponDetailDto
            {
                CouponId = coupon.CouponId,
                CouponCode = coupon.CouponCode,
                CouponName = coupon.CouponName,
                Description = coupon.Description,
                StartTime = coupon.StartTime,
                EndTime = coupon.EndTime,
                DiscountValue = coupon.DiscountValue,
                MaxUsage = coupon.MaxUsage,
                UsedCount = coupon.UsedCount,
                Type = coupon.TypeLv.ValueCode,
                CouponStatus = coupon.CouponStatusLv.ValueCode,
                CreatedAt = coupon.CreatedAt
            };
        }

        public async Task<CouponDTO> CreateCouponAsync(CreateCouponRequest request, CancellationToken ct)
        {
            // Normalize coupon code: remove all whitespace and convert to uppercase
            request.CouponCode = string.Concat(request.CouponCode.Split()).ToUpper();

            // Validate coupon code uniqueness
            var existingCoupon = await _couponRepository.GetByCodeAsync(request.CouponCode, ct);
            if (existingCoupon != null)
                throw new InvalidOperationException($"Coupon with code '{request.CouponCode}' already exists.");

            // Validate date range
            if (request.EndTime <= request.StartTime)
                throw new InvalidOperationException("End time must be after start time.");

            // Validate discount value for percent type
            if (request.Type == "PERCENT" && (request.DiscountValue < 0 || request.DiscountValue > 100))
                throw new InvalidOperationException("Discount percentage must be between 0 and 100%.");

            // Get lookup value IDs (throws KeyNotFoundException if not found)
            var typeLvId = await _lookupResolver.GetIdAsync(
                (ushort)LookupTypeEnum.CouponType,
                request.Type,
                ct);

            var status = CalculateStatus(request.StartTime, request.EndTime);
            var statusLvId = await _lookupResolver.GetIdAsync(
                (ushort)LookupTypeEnum.CouponStatus,
                status,
                ct);

            var coupon = new Coupon
            {
                CouponCode = request.CouponCode,
                CouponName = request.CouponName.Trim(),
                Description = request.Description?.Trim(),
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                DiscountValue = request.DiscountValue,
                MaxUsage = request.MaxUsage,
                UsedCount = 0,
                TypeLvId = typeLvId,
                CouponStatusLvId = statusLvId,
                CreatedAt = DateTime.UtcNow
            };

            var createdCoupon = await _couponRepository.CreateAsync(coupon, ct);

            return new CouponDTO
            {
                CouponId = createdCoupon.CouponId,
                CouponCode = createdCoupon.CouponCode,
                CouponName = createdCoupon.CouponName,
                StartTime = createdCoupon.StartTime,
                EndTime = createdCoupon.EndTime,
                DiscountValue = createdCoupon.DiscountValue,
                MaxUsage = createdCoupon.MaxUsage,
                UsedCount = createdCoupon.UsedCount,
                Type = createdCoupon.TypeLv.ValueCode,
                CouponStatus = createdCoupon.CouponStatusLv.ValueCode
            };
        }

        public async Task<CouponDTO> UpdateCouponAsync(long id, UpdateCouponRequest request, CancellationToken ct)
        {
            var coupon = await _couponRepository.GetByIdAsync(id, ct);
            if (coupon == null)
                throw new KeyNotFoundException($"Coupon with ID {id} not found.");

            // Normalize coupon code: remove all whitespace and convert to uppercase
            request.CouponCode = string.Concat(request.CouponCode.Split()).ToUpper();

            // Validate coupon code uniqueness (if changed)
            if (coupon.CouponCode.ToLower() != request.CouponCode.ToLower())
            {
                var existingCoupon = await _couponRepository.GetByCodeAsync(request.CouponCode, ct);
                if (existingCoupon != null)
                    throw new InvalidOperationException($"Coupon with code '{request.CouponCode}' already exists.");
            }

            // Validate date range
            if (request.EndTime <= request.StartTime)
                throw new InvalidOperationException("End time must be after start time.");

            // Validate discount value for percent type
            if (request.Type == "PERCENT" && (request.DiscountValue < 0 || request.DiscountValue > 100))
                throw new InvalidOperationException("Discount percentage must be between 0 and 100%.");

            // Get lookup value IDs (throws KeyNotFoundException if not found)
            var typeLvId = await _lookupResolver.GetIdAsync(
                (ushort)LookupTypeEnum.CouponType,
                request.Type,
                ct);

            // Update coupon — status is not changed on update (use disable/activate endpoints)
            coupon.CouponCode = request.CouponCode;
            coupon.CouponName = request.CouponName.Trim();
            coupon.Description = request.Description?.Trim();
            coupon.StartTime = request.StartTime;
            coupon.EndTime = request.EndTime;
            coupon.DiscountValue = request.DiscountValue;
            coupon.MaxUsage = request.MaxUsage;
            coupon.TypeLvId = typeLvId;

            var updatedCoupon = await _couponRepository.UpdateAsync(coupon, ct);

            return new CouponDTO
            {
                CouponId = updatedCoupon.CouponId,
                CouponCode = updatedCoupon.CouponCode,
                CouponName = updatedCoupon.CouponName,
                StartTime = updatedCoupon.StartTime,
                EndTime = updatedCoupon.EndTime,
                DiscountValue = updatedCoupon.DiscountValue,
                MaxUsage = updatedCoupon.MaxUsage,
                UsedCount = updatedCoupon.UsedCount,
                Type = updatedCoupon.TypeLv.ValueCode,
                CouponStatus = updatedCoupon.CouponStatusLv.ValueCode
            };
        }

        public async Task DeleteCouponAsync(long id, CancellationToken ct)
        {
            var coupon = await _couponRepository.GetByIdAsync(id, ct);
            if (coupon == null)
                throw new KeyNotFoundException($"Coupon with ID {id} not found.");

            // Check if coupon has been used
            if (coupon.UsedCount > 0)
                throw new InvalidOperationException("Cannot delete coupon that has been used.");

            var deleted = await _couponRepository.DeleteAsync(id, ct);
            if (!deleted)
                throw new KeyNotFoundException($"Failed to delete coupon with ID {id}.");
        }

        public async Task DisableCouponAsync(long id, CancellationToken ct)
        {
            var coupon = await _couponRepository.GetByIdAsync(id, ct);
            if (coupon == null)
                throw new KeyNotFoundException($"Coupon with ID {id} not found.");

            var disableStatusId = await _lookupResolver.GetIdAsync(
                (ushort)LookupTypeEnum.CouponStatus,
                CouponStatusCode.DISABLED,
                ct);

            coupon.CouponStatusLvId = disableStatusId;
            await _couponRepository.UpdateAsync(coupon, ct);
        }

        public async Task ActivateCouponAsync(long id, CancellationToken ct)
        {
            var coupon = await _couponRepository.GetByIdAsync(id, ct);
            if (coupon == null)
                throw new KeyNotFoundException($"Coupon with ID {id} not found.");

            var status = CalculateStatus(coupon.StartTime, coupon.EndTime);
            var statusId = await _lookupResolver.GetIdAsync(
                (ushort)LookupTypeEnum.CouponStatus,
                status,
                ct);

            coupon.CouponStatusLvId = statusId;
            await _couponRepository.UpdateAsync(coupon, ct);
        }

        private CouponStatusCode CalculateStatus(DateTime start, DateTime end)
        {
            var now = DateTime.UtcNow;

            if (now < start)
                return CouponStatusCode.SCHEDULED;

            if (now >= start && now <= end)
                return CouponStatusCode.ACTIVE;

            return CouponStatusCode.EXPIRED;
        }
    }
}
