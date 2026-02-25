using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infa.Data.Configurations;

public sealed class PromotionRuleConfiguration : IEntityTypeConfiguration<PromotionRule>
{
    public void Configure(EntityTypeBuilder<PromotionRule> entity)
    {
        entity.HasKey(e => e.RuleId).HasName("PRIMARY");

        entity.ToTable("promotion_rule");

        entity.HasIndex(e => e.PromotionId, "promotion_id");

        entity.HasIndex(e => e.RequiredCategoryId, "required_category_id");

        entity.HasIndex(e => e.RequiredDishId, "required_dish_id");

        entity.Property(e => e.RuleId).HasColumnName("rule_id");
        entity.Property(e => e.MinOrderValue)
        .HasPrecision(14, 2)
        .HasColumnName("min_order_value");
        entity.Property(e => e.MinQuantity).HasColumnName("min_quantity");
        entity.Property(e => e.PromotionId).HasColumnName("promotion_id");
        entity.Property(e => e.RequiredCategoryId).HasColumnName("required_category_id");
        entity.Property(e => e.RequiredDishId).HasColumnName("required_dish_id");

        entity.HasOne(d => d.Promotion).WithMany(p => p.PromotionRules)
        .HasForeignKey(d => d.PromotionId)
        .OnDelete(DeleteBehavior.ClientSetNull)
        .HasConstraintName("promotion_rule_ibfk_1");

        entity.HasOne(d => d.RequiredCategory).WithMany(p => p.PromotionRules)
        .HasForeignKey(d => d.RequiredCategoryId)
        .HasConstraintName("promotion_rule_ibfk_3");

        entity.HasOne(d => d.RequiredDish).WithMany(p => p.PromotionRules)
        .HasForeignKey(d => d.RequiredDishId)
        .HasConstraintName("promotion_rule_ibfk_2");
    }
}
