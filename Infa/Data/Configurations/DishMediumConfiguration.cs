using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infa.Data.Configurations;

public sealed class DishMediumConfiguration : IEntityTypeConfiguration<DishMedium>
{
    public void Configure(EntityTypeBuilder<DishMedium> entity)
    {
        entity.HasKey(e => new { e.DishId, e.MediaId })
        .HasName("PRIMARY")
        .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });

        entity.ToTable("dish_media");

        entity.HasIndex(e => e.MediaId, "media_id");

        entity.Property(e => e.DishId).HasColumnName("dish_id");
        entity.Property(e => e.MediaId).HasColumnName("media_id");
        entity.Property(e => e.IsPrimary)
        .HasDefaultValueSql("'0'")
        .HasColumnName("is_primary");

        entity.HasOne(d => d.Dish).WithMany(p => p.DishMedia)
        .HasForeignKey(d => d.DishId)
        .OnDelete(DeleteBehavior.ClientSetNull)
        .HasConstraintName("dish_media_ibfk_1");

        entity.HasOne(d => d.Media).WithMany(p => p.DishMedia)
        .HasForeignKey(d => d.MediaId)
        .OnDelete(DeleteBehavior.ClientSetNull)
        .HasConstraintName("dish_media_ibfk_2");
    }
}
