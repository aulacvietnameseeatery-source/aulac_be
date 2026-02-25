using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infa.Data.Configurations;

public sealed class DishTagConfiguration : IEntityTypeConfiguration<DishTag>
{
    public void Configure(EntityTypeBuilder<DishTag> entity)
    {
        entity.HasKey(e => e.DishTagId).HasName("PRIMARY");

        entity.ToTable("dish_tag");

        entity.HasIndex(e => e.DishId, "FK_dish_tag_dish_dish_id");

        entity.HasIndex(e => e.TagId, "FK_dish_tag_lookup_value_value_id");

        entity.Property(e => e.DishId).HasColumnName("dish_id");
        entity.Property(e => e.DishTagId).HasColumnName("dish_tag_id");
        entity.Property(e => e.TagId).HasColumnName("tag_id");

        entity.HasOne(d => d.Dish).WithMany()
        .HasForeignKey(d => d.DishId)
        .OnDelete(DeleteBehavior.ClientSetNull);

        entity.HasOne(d => d.Tag).WithMany()
        .HasForeignKey(d => d.TagId)
        .OnDelete(DeleteBehavior.ClientSetNull)
        .HasConstraintName("FK_dish_tag_lookup_value_value_id");
    }
}
