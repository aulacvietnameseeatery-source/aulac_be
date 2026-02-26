using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infa.Data.Configurations;

public sealed class InventoryTransactionConfiguration : IEntityTypeConfiguration<InventoryTransaction>
{
    public void Configure(EntityTypeBuilder<InventoryTransaction> entity)
    {
        entity.HasKey(e => e.TransactionId).HasName("PRIMARY");

        entity.ToTable("inventory_transaction");

        entity.HasIndex(e => e.SupplierId, "FK_inventory_transaction_supplier_supplier_id");

        entity.HasIndex(e => e.CreatedBy, "fk_inventory_transaction_staff");

        entity.HasIndex(e => e.CreatedAt, "idx_inventory_transaction_dir_time");

        entity.HasIndex(e => e.StatusLvId, "idx_inventory_tx_status_lv");

        entity.HasIndex(e => e.TypeLvId, "idx_inventory_tx_type_lv");

        entity.Property(e => e.TransactionId).HasColumnName("transaction_id");
        entity.Property(e => e.CreatedAt)
        .HasDefaultValueSql("CURRENT_TIMESTAMP")
        .HasColumnType("datetime")
        .HasColumnName("created_at");
        entity.Property(e => e.CreatedBy).HasColumnName("created_by");
        entity.Property(e => e.Note)
        .HasMaxLength(255)
        .HasColumnName("note");
        entity.Property(e => e.StatusLvId).HasColumnName("status_lv_id");
        entity.Property(e => e.SupplierId).HasColumnName("supplier_id");
        entity.Property(e => e.TypeLvId).HasColumnName("type_lv_id");

        entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.InventoryTransactions)
        .HasForeignKey(d => d.CreatedBy)
        .HasConstraintName("fk_inventory_transaction_staff");

        entity.HasOne(d => d.StatusLv).WithMany(p => p.InventoryTransactionStatusLvs)
        .HasForeignKey(d => d.StatusLvId)
        .OnDelete(DeleteBehavior.ClientSetNull)
        .HasConstraintName("fk_inventory_tx_status_lv");

        entity.HasOne(d => d.Supplier).WithMany(p => p.InventoryTransactions)
        .HasForeignKey(d => d.SupplierId);

        entity.HasOne(d => d.TypeLv).WithMany(p => p.InventoryTransactionTypeLvs)
        .HasForeignKey(d => d.TypeLvId)
        .OnDelete(DeleteBehavior.ClientSetNull)
        .HasConstraintName("fk_inventory_tx_type_lv");
    }
}
