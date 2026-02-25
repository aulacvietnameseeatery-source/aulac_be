using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infa.Data.Configurations;

public sealed class RecipeConfiguration : IEntityTypeConfiguration<Recipe>
{
    public void Configure(EntityTypeBuilder<Recipe> entity)
    {
        entity.HasKey(e => new { e.DishId, e.IngredientId })
        .HasName("PRIMARY")
        .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });

        entity.ToTable("recipe");

        entity.HasIndex(e => e.IngredientId, "idx_recipe_ingredient");

        entity.Property(e => e.DishId).HasColumnName("dish_id");
        entity.Property(e => e.IngredientId).HasColumnName("ingredient_id");
        entity.Property(e => e.Note)
        .HasMaxLength(255)
        .HasColumnName("note");
        entity.Property(e => e.Quantity)
        .HasPrecision(12, 3)
        .HasColumnName("quantity");
        entity.Property(e => e.Unit)
        .HasMaxLength(20)
        .HasColumnName("unit");

        entity.HasOne(d => d.Dish).WithMany(p => p.Recipes)
        .HasForeignKey(d => d.DishId)
        .OnDelete(DeleteBehavior.ClientSetNull)
        .HasConstraintName("fk_recipe_dish");

        entity.HasOne(d => d.Ingredient).WithMany(p => p.Recipes)
        .HasForeignKey(d => d.IngredientId)
        .OnDelete(DeleteBehavior.ClientSetNull)
        .HasConstraintName("fk_recipe_ingredient");
    }
}
