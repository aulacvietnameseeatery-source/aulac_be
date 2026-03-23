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

        entity.HasIndex(e => e.UnitLvId, "idx_inventory_tx_item_unit_lv");

        entity.HasIndex(e => e.VarianceReasonLvId, "idx_inventory_tx_item_variance_reason_lv");

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
        entity.Property(e => e.UnitLvId).HasColumnName("unit_lv_id");

        entity.Property(e => e.UnitPrice)
        .HasPrecision(14, 2)
        .HasColumnName("unit_price");

        entity.Property(e => e.SystemQuantity)
        .HasPrecision(14, 3)
        .HasColumnName("system_quantity");

        entity.Property(e => e.ActualQuantity)
        .HasPrecision(14, 3)
        .HasColumnName("actual_quantity");

        entity.Property(e => e.VarianceReasonLvId).HasColumnName("variance_reason_lv_id");

        entity.HasOne(d => d.Ingredient).WithMany(p => p.InventoryTransactionItems)
        .HasForeignKey(d => d.IngredientId)
        .OnDelete(DeleteBehavior.ClientSetNull)
        .HasConstraintName("fk_inventory_transaction_item_ingredient");

        entity.HasOne(d => d.Transaction).WithMany(p => p.InventoryTransactionItems)
        .HasForeignKey(d => d.TransactionId)
        .HasConstraintName("fk_inventory_transaction_item_transaction");

        entity.HasOne(d => d.UnitLv)
        .WithMany()
        .HasForeignKey(d => d.UnitLvId)
        .OnDelete(DeleteBehavior.ClientSetNull)
        .HasConstraintName("fk_inventory_tx_item_unit_lv");

        entity.HasOne(d => d.VarianceReasonLv)
        .WithMany()
        .HasForeignKey(d => d.VarianceReasonLvId)
        .HasConstraintName("fk_inventory_tx_item_variance_reason_lv");
    }
}
