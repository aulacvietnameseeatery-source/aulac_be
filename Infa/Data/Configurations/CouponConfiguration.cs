using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infa.Data.Configurations;

public sealed class CouponConfiguration : IEntityTypeConfiguration<Coupon>
{
    public void Configure(EntityTypeBuilder<Coupon> entity)
    {
        entity.HasKey(e => e.CouponId).HasName("PRIMARY");

        entity.ToTable("coupon");

        entity.HasIndex(e => e.CouponStatusLvId, "idx_coupon_status_lv");

        entity.HasIndex(e => e.TypeLvId, "idx_coupon_type_lv");

        entity.HasIndex(e => e.CouponCode, "uq_coupon_code").IsUnique();

        entity.Property(e => e.CouponId).HasColumnName("coupon_id");
        entity.Property(e => e.CouponCode)
        .HasMaxLength(50)
        .HasColumnName("coupon_code");
        entity.Property(e => e.CouponName)
        .HasMaxLength(200)
        .HasColumnName("coupon_name");
        entity.Property(e => e.CouponStatusLvId).HasColumnName("coupon_status_lv_id");
        entity.Property(e => e.CreatedAt)
        .HasDefaultValueSql("CURRENT_TIMESTAMP")
        .HasColumnType("datetime")
        .HasColumnName("created_at");
        entity.Property(e => e.Description)
        .HasMaxLength(255)
        .HasColumnName("description");
        entity.Property(e => e.DiscountValue)
        .HasPrecision(10, 2)
        .HasColumnName("discount_value");
        entity.Property(e => e.EndTime)
        .HasColumnType("datetime")
        .HasColumnName("end_time");
        entity.Property(e => e.MaxUsage).HasColumnName("max_usage");
        entity.Property(e => e.StartTime)
        .HasColumnType("datetime")
        .HasColumnName("start_time");
        entity.Property(e => e.TypeLvId).HasColumnName("type_lv_id");
        entity.Property(e => e.UsedCount)
        .HasDefaultValueSql("'0'")
        .HasColumnName("used_count");

        entity.HasOne(d => d.CouponStatusLv).WithMany(p => p.CouponCouponStatusLvs)
        .HasForeignKey(d => d.CouponStatusLvId)
        .OnDelete(DeleteBehavior.ClientSetNull)
        .HasConstraintName("fk_coupon_status_lv");

        entity.HasOne(d => d.TypeLv).WithMany(p => p.CouponTypeLvs)
        .HasForeignKey(d => d.TypeLvId)
        .OnDelete(DeleteBehavior.ClientSetNull)
        .HasConstraintName("fk_coupon_type_lv");
    }
}
