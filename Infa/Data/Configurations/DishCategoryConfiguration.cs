using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infa.Data.Configurations;

public sealed class DishCategoryConfiguration : IEntityTypeConfiguration<DishCategory>
{
    public void Configure(EntityTypeBuilder<DishCategory> entity)
    {
        entity.HasKey(e => e.CategoryId).HasName("PRIMARY");

        entity.ToTable("dish_category");

        entity.HasIndex(e => e.DescriptionTextId, "FK_dish_category_i18n_text_text_id");

        entity.HasIndex(e => e.CategoryNameTextId, "fk_cat_name_text");

        entity.Property(e => e.CategoryId).HasColumnName("category_id");
        entity.Property(e => e.CategoryName)
        .HasMaxLength(100)
        .HasColumnName("category_name");
        entity.Property(e => e.CategoryNameTextId).HasColumnName("category_name_text_id");
        entity.Property(e => e.Description)
        .HasMaxLength(100)
        .HasColumnName("description");
        entity.Property(e => e.DescriptionTextId).HasColumnName("description_text_id");
        entity.Property(e => e.IsDisabled).HasColumnName("is_disable");

        entity.Property(e => e.DisPlayOrder).HasColumnName("display_order");

        entity.HasOne(d => d.CategoryNameText).WithMany(p => p.DishCategoryCategoryNameTexts)
        .HasForeignKey(d => d.CategoryNameTextId)
        .HasConstraintName("fk_cat_name_text");

        entity.HasOne(d => d.DescriptionText).WithMany(p => p.DishCategoryDescriptionTexts)
        .HasForeignKey(d => d.DescriptionTextId)
        .HasConstraintName("FK_dish_category_i18n_text_text_id");
    }
}
