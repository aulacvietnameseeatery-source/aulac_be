using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infa.Data.Configurations;

public sealed class PromotionConfiguration : IEntityTypeConfiguration<Promotion>
{
    public void Configure(EntityTypeBuilder<Promotion> entity)
    {
        entity.HasKey(e => e.PromotionId).HasName("PRIMARY");

        entity.ToTable("promotion");

        entity.HasIndex(e => e.PromoDescTextId, "fk_promo_desc_text");

        entity.HasIndex(e => e.PromoNameTextId, "fk_promo_name_text");

        entity.HasIndex(e => e.PromotionStatusLvId, "idx_promotion_status_lv");

        entity.HasIndex(e => e.TypeLvId, "idx_promotion_type_lv");

        entity.HasIndex(e => e.PromoCode, "promo_code").IsUnique();

        entity.Property(e => e.PromotionId).HasColumnName("promotion_id");
        entity.Property(e => e.CreatedAt)
        .HasDefaultValueSql("CURRENT_TIMESTAMP")
        .HasColumnType("datetime")
        .HasColumnName("created_at");
        entity.Property(e => e.Description)
        .HasMaxLength(255)
        .HasColumnName("description");
        entity.Property(e => e.EndTime)
        .HasColumnType("datetime")
        .HasColumnName("end_time");
        entity.Property(e => e.MaxUsage).HasColumnName("max_usage");
        entity.Property(e => e.PromoCode)
        .HasMaxLength(50)
        .HasColumnName("promo_code");
        entity.Property(e => e.PromoDescTextId).HasColumnName("promo_desc_text_id");
        entity.Property(e => e.PromoName)
        .HasMaxLength(200)
        .HasColumnName("promo_name");
        entity.Property(e => e.PromoNameTextId).HasColumnName("promo_name_text_id");
        entity.Property(e => e.PromotionStatusLvId).HasColumnName("promotion_status_lv_id");
        entity.Property(e => e.StartTime)
        .HasColumnType("datetime")
        .HasColumnName("start_time");
        entity.Property(e => e.TypeLvId).HasColumnName("type_lv_id");
        entity.Property(e => e.DiscountValue)
        .HasPrecision(10, 2)
        .HasColumnName("discount_value");
        entity.Property(e => e.UsedCount)
        .HasDefaultValueSql("'0'")
        .HasColumnName("used_count");

        entity.HasOne(d => d.PromoDescText).WithMany(p => p.PromotionPromoDescTexts)
        .HasForeignKey(d => d.PromoDescTextId)
        .HasConstraintName("fk_promo_desc_text");

        entity.HasOne(d => d.PromoNameText).WithMany(p => p.PromotionPromoNameTexts)
        .HasForeignKey(d => d.PromoNameTextId)
        .HasConstraintName("fk_promo_name_text");

        entity.HasOne(d => d.PromotionStatusLv).WithMany(p => p.PromotionPromotionStatusLvs)
        .HasForeignKey(d => d.PromotionStatusLvId)
        .OnDelete(DeleteBehavior.ClientSetNull)
        .HasConstraintName("fk_promotion_status_lv");

        entity.HasOne(d => d.TypeLv).WithMany(p => p.PromotionTypeLvs)
        .HasForeignKey(d => d.TypeLvId)
        .OnDelete(DeleteBehavior.ClientSetNull)
        .HasConstraintName("fk_promotion_type_lv");
    }
}
