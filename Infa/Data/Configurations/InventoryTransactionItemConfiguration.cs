using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infa.Data.Configurations;

public sealed class InventoryTransactionItemConfiguration : IEntityTypeConfiguration<InventoryTransactionItem>
{
    public void Configure(EntityTypeBuilder<InventoryTransactionItem> entity)
    {
        entity.HasKey(e => e.TransactionItemId).HasName("PRIMARY");

        entity.ToTable("inventory_transaction_item");

        entity.HasIndex(e => e.IngredientId, "idx_inventory_transaction_item_ingredient");

        entity.HasIndex(e => e.TransactionId, "idx_inventory_transaction_item_transaction");

        entity.HasIndex(e => new { e.TransactionId, e.IngredientId }, "uq_inventory_transaction_item").IsUnique();

        entity.Property(e => e.TransactionItemId).HasColumnName("transaction_item_id");
        entity.Property(e => e.IngredientId).HasColumnName("ingredient_id");
        entity.Property(e => e.Note)
        .HasMaxLength(255)
        .HasColumnName("note");
        entity.Property(e => e.Quantity)
        .HasPrecision(14, 3)
        .HasColumnName("quantity");
        entity.Property(e => e.TransactionId).HasColumnName("transaction_id");
        entity.Property(e => e.Unit)
        .HasMaxLength(20)
        .HasColumnName("unit");

        entity.HasOne(d => d.Ingredient).WithMany(p => p.InventoryTransactionItems)
        .HasForeignKey(d => d.IngredientId)
        .OnDelete(DeleteBehavior.ClientSetNull)
        .HasConstraintName("fk_inventory_transaction_item_ingredient");

        entity.HasOne(d => d.Transaction).WithMany(p => p.InventoryTransactionItems)
        .HasForeignKey(d => d.TransactionId)
        .HasConstraintName("fk_inventory_transaction_item_transaction");
    }
}
