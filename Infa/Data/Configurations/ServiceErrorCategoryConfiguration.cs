using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infa.Data.Configurations;

public sealed class ServiceErrorCategoryConfiguration : IEntityTypeConfiguration<ServiceErrorCategory>
{
    public void Configure(EntityTypeBuilder<ServiceErrorCategory> entity)
    {
        entity.HasKey(e => e.CategoryId).HasName("PRIMARY");

        entity.ToTable("service_error_category");

        entity.HasIndex(e => e.CategoryCode, "category_code").IsUnique();

        entity.HasIndex(e => e.CategoryDescTextId, "fk_sec_desc_text");

        entity.HasIndex(e => e.CategoryNameTextId, "fk_sec_name_text");

        entity.Property(e => e.CategoryId).HasColumnName("category_id");
        entity.Property(e => e.CategoryCode)
        .HasMaxLength(50)
        .HasColumnName("category_code");
        entity.Property(e => e.CategoryDescTextId).HasColumnName("category_desc_text_id");
        entity.Property(e => e.CategoryName)
        .HasMaxLength(150)
        .HasColumnName("category_name");
        entity.Property(e => e.CategoryNameTextId).HasColumnName("category_name_text_id");
        entity.Property(e => e.Description)
        .HasMaxLength(255)
        .HasColumnName("description");

        entity.HasOne(d => d.CategoryDescText).WithMany(p => p.ServiceErrorCategoryCategoryDescTexts)
        .HasForeignKey(d => d.CategoryDescTextId)
        .HasConstraintName("fk_sec_desc_text");

        entity.HasOne(d => d.CategoryNameText).WithMany(p => p.ServiceErrorCategoryCategoryNameTexts)
        .HasForeignKey(d => d.CategoryNameTextId)
        .HasConstraintName("fk_sec_name_text");
    }
}
