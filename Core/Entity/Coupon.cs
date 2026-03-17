using System;
using System.Collections.Generic;

namespace Core.Entity;

public partial class Coupon
{
    public long CouponId { get; set; }

    public string CouponCode { get; set; } = null!;

    public string CouponName { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public decimal DiscountValue { get; set; }

    public int? MaxUsage { get; set; }

    public int? UsedCount { get; set; }

    public DateTime? CreatedAt { get; set; }

    public uint TypeLvId { get; set; }

    public uint CouponStatusLvId { get; set; }

    public virtual LookupValue CouponStatusLv { get; set; } = null!;

    public virtual ICollection<OrderCoupon> OrderCoupons { get; set; } = new List<OrderCoupon>();

    public virtual LookupValue TypeLv { get; set; } = null!;
}
