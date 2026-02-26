using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infa.Data.Configurations;

public sealed class OrderPromotionConfiguration : IEntityTypeConfiguration<OrderPromotion>
{
    public void Configure(EntityTypeBuilder<OrderPromotion> entity)
    {
        entity.HasKey(e => e.OrderPromotionId).HasName("PRIMARY");

        entity.ToTable("order_promotion");

        entity.HasIndex(e => e.PromotionId, "idx_invoice_promo_promo");

        entity.HasIndex(e => new { e.OrderId, e.PromotionId }, "uq_invoice_promo").IsUnique();

        entity.Property(e => e.OrderPromotionId).HasColumnName("order_promotion_id");
        entity.Property(e => e.AppliedAt)
        .HasDefaultValueSql("CURRENT_TIMESTAMP")
        .HasColumnType("datetime")
        .HasColumnName("applied_at");
        entity.Property(e => e.DiscountAmount)
        .HasPrecision(14, 2)
        .HasColumnName("discount_amount");
        entity.Property(e => e.OrderId).HasColumnName("order_id");
        entity.Property(e => e.PromotionId).HasColumnName("promotion_id");

        entity.HasOne(d => d.Order).WithMany(p => p.OrderPromotions)
        .HasForeignKey(d => d.OrderId)
        .OnDelete(DeleteBehavior.ClientSetNull)
        .HasConstraintName("FK_invoice_promotion_order_id");

        entity.HasOne(d => d.Promotion).WithMany(p => p.OrderPromotions)
        .HasForeignKey(d => d.PromotionId)
        .OnDelete(DeleteBehavior.ClientSetNull)
        .HasConstraintName("order_promotion_ibfk_2");
    }
}
