using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infa.Data.Configurations;

public sealed class OrderCouponConfiguration : IEntityTypeConfiguration<OrderCoupon>
{
    public void Configure(EntityTypeBuilder<OrderCoupon> entity)
    {
        entity.HasKey(e => e.OrderCouponId).HasName("PRIMARY");

        entity.ToTable("order_coupon");

        entity.HasIndex(e => e.CouponId, "idx_order_coupon_coupon");

        entity.HasIndex(e => new { e.OrderId, e.CouponId }, "uq_order_coupon").IsUnique();

        entity.Property(e => e.OrderCouponId).HasColumnName("order_coupon_id");
        entity.Property(e => e.AppliedAt)
        .HasDefaultValueSql("CURRENT_TIMESTAMP")
        .HasColumnType("datetime")
        .HasColumnName("applied_at");
        entity.Property(e => e.CouponId).HasColumnName("coupon_id");
        entity.Property(e => e.DiscountAmount)
        .HasPrecision(14, 2)
        .HasColumnName("discount_amount");
        entity.Property(e => e.OrderId).HasColumnName("order_id");

        entity.HasOne(d => d.Coupon).WithMany(p => p.OrderCoupons)
        .HasForeignKey(d => d.CouponId)
        .OnDelete(DeleteBehavior.ClientSetNull)
        .HasConstraintName("fk_order_coupon_coupon");

        entity.HasOne(d => d.Order).WithMany(p => p.OrderCoupons)
        .HasForeignKey(d => d.OrderId)
        .OnDelete(DeleteBehavior.ClientSetNull)
        .HasConstraintName("fk_order_coupon_order");
    }
}
