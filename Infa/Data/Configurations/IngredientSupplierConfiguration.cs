using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infa.Data.Configurations;

public sealed class IngredientSupplierConfiguration : IEntityTypeConfiguration<IngredientSupplier>
{
    public void Configure(EntityTypeBuilder<IngredientSupplier> entity)
    {
        entity.HasKey(e => e.IngredientSupplierId).HasName("PRIMARY");

        entity.ToTable("ingredient_supplier");

        entity.HasIndex(e => e.IngredientId, "FK_ingredient_supplier_ingredient_ingredient_id");

        entity.HasIndex(e => e.SupplierId, "FK_ingredient_supplier_supplier_supplier_id");

        entity.Property(e => e.IngredientSupplierId)
        .ValueGeneratedOnAdd()
        .HasColumnName("ingredient_supplier_id");
        entity.Property(e => e.CreatedAt)
        .HasColumnType("datetime")
        .HasColumnName("created_at");
        entity.Property(e => e.IngredientId).HasColumnName("ingredient_id");
        entity.Property(e => e.SupplierId).HasColumnName("supplier_id");

        entity.HasOne(d => d.Ingredient).WithMany(p => p.IngredientSuppliers)
        .HasForeignKey(d => d.IngredientId);

        entity.HasOne(d => d.Supplier).WithMany(p => p.IngredientSuppliers)
        .HasForeignKey(d => d.SupplierId);
    }
}
