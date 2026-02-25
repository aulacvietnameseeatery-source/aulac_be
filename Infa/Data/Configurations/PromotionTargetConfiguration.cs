using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infa.Data.Configurations;

public sealed class PromotionTargetConfiguration : IEntityTypeConfiguration<PromotionTarget>
{
    public void Configure(EntityTypeBuilder<PromotionTarget> entity)
    {
        entity.HasKey(e => e.TargetId).HasName("PRIMARY");

        entity.ToTable("promotion_target");

        entity.HasIndex(e => e.CategoryId, "category_id");

        entity.HasIndex(e => e.DishId, "dish_id");

        entity.HasIndex(e => e.PromotionId, "promotion_id");

        entity.Property(e => e.TargetId).HasColumnName("target_id");
        entity.Property(e => e.CategoryId).HasColumnName("category_id");
        entity.Property(e => e.DishId).HasColumnName("dish_id");
        entity.Property(e => e.PromotionId).HasColumnName("promotion_id");

        entity.HasOne(d => d.Category).WithMany(p => p.PromotionTargets)
        .HasForeignKey(d => d.CategoryId)
        .HasConstraintName("promotion_target_ibfk_3");

        entity.HasOne(d => d.Dish).WithMany(p => p.PromotionTargets)
        .HasForeignKey(d => d.DishId)
        .HasConstraintName("promotion_target_ibfk_2");

        entity.HasOne(d => d.Promotion).WithMany(p => p.PromotionTargets)
        .HasForeignKey(d => d.PromotionId)
        .OnDelete(DeleteBehavior.ClientSetNull)
        .HasConstraintName("promotion_target_ibfk_1");
    }
}
