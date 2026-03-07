using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infa.Data.Configurations;

public sealed class CurrentStockConfiguration : IEntityTypeConfiguration<CurrentStock>
{
    public void Configure(EntityTypeBuilder<CurrentStock> entity)
    {
        entity.HasKey(e => e.IngredientId).HasName("PRIMARY");

        entity.ToTable("current_stock");

        entity.Property(e => e.IngredientId)
            .ValueGeneratedNever()
            .HasColumnName("ingredient_id");

        entity.Property(e => e.LastUpdatedAt)
            .ValueGeneratedOnAddOrUpdate()
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .HasColumnType("datetime")
            .HasColumnName("last_updated_at");

        entity.Property(e => e.QuantityOnHand)
            .HasPrecision(14, 3)
            .HasColumnName("quantity_on_hand");

        entity.Property(e => e.MinStockLevel)
            .HasPrecision(14, 3) 
            .HasColumnName("min_stock_level") 
            .HasDefaultValue(0m); 

        entity.HasOne(d => d.Ingredient).WithOne(p => p.CurrentStock)
            .HasForeignKey<CurrentStock>(d => d.IngredientId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("fk_current_stock_ingredient");
    }
}