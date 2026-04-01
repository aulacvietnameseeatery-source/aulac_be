using System;

namespace Core.DTO.Coupon
{
    public class CouponDTO
    {
        public long CouponId { get; set; }

        public string CouponCode { get; set; } = string.Empty;
        public string CouponName { get; set; } = string.Empty;
        public string? CustomerName { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public decimal DiscountValue { get; set; }

        public int? MaxUsage { get; set; }
        public int? UsedCount { get; set; }

        public string Type { get; set; } = string.Empty;

        public string CouponStatus { get; set; } = string.Empty;
    }
}
