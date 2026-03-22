using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infa.Data.Configurations;

public sealed class IngredientConfiguration : IEntityTypeConfiguration<Ingredient>
{
    public void Configure(EntityTypeBuilder<Ingredient> entity)
    {
        entity.HasKey(e => e.IngredientId).HasName("PRIMARY");

        entity.ToTable("ingredient");

        entity.HasIndex(e => e.ImageId, "FK_ingredient_image_id");
        entity.HasIndex(e => e.TypeLvId, "FK_ingredient_type_lv_id");
        entity.HasIndex(e => e.IngredientNameTextId, "fk_ingredient_name_text");

        entity.HasIndex(e => e.UnitLvId, "FK_ingredient_unit_lv_id");

        entity.Property(e => e.IngredientId).HasColumnName("ingredient_id");
        entity.Property(e => e.ImageId).HasColumnName("image_id");

        entity.Property(e => e.IngredientName)
        .HasMaxLength(200)
        .HasColumnName("ingredient_name");

        entity.Property(e => e.IngredientNameTextId).HasColumnName("ingredient_name_text_id");
        entity.Property(e => e.TypeLvId).HasColumnName("type_lv_id");

        entity.Property(e => e.UnitLvId).HasColumnName("unit_lv_id");

        entity.HasOne(d => d.Image).WithMany(p => p.Ingredients)
        .HasForeignKey(d => d.ImageId)
        .HasConstraintName("FK_ingredient_image_id");

        entity.HasOne(d => d.IngredientNameText).WithMany(p => p.Ingredients)
        .HasForeignKey(d => d.IngredientNameTextId)
        .HasConstraintName("fk_ingredient_name_text");

        entity.HasOne(d => d.TypeLv).WithMany(p => p.Ingredients)
        .HasForeignKey(d => d.TypeLvId)
        .HasConstraintName("FK_ingredient_type_lv_id");

        entity.HasOne(d => d.UnitLv)
        .WithMany() 
        .HasForeignKey(d => d.UnitLvId)
        .HasConstraintName("FK_ingredient_unit_lv_id");
    }
}