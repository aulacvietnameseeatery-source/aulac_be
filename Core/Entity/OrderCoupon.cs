using System;

namespace Core.Entity;

public partial class OrderCoupon
{
    public long OrderCouponId { get; set; }

    public long OrderId { get; set; }

    public long CouponId { get; set; }

    public decimal DiscountAmount { get; set; }

    public DateTime? AppliedAt { get; set; }

    public virtual Coupon Coupon { get; set; } = null!;

    public virtual Order Order { get; set; } = null!;
}
