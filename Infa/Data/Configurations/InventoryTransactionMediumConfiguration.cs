using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infa.Data.Configurations;

public sealed class InventoryTransactionMediumConfiguration : IEntityTypeConfiguration<InventoryTransactionMedium>
{
    public void Configure(EntityTypeBuilder<InventoryTransactionMedium> entity)
    {
        entity.HasKey(e => new { e.TransactionId, e.MediaId })
        .HasName("PRIMARY")
        .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });

        entity.ToTable("inventory_transaction_media");

        entity.HasIndex(e => e.MediaId, "idx_inventory_transaction_media_media");

        entity.HasIndex(e => new { e.TransactionId, e.IsPrimary }, "idx_inventory_transaction_media_primary");

        entity.Property(e => e.TransactionId).HasColumnName("transaction_id");
        entity.Property(e => e.MediaId).HasColumnName("media_id");
        entity.Property(e => e.IsPrimary)
        .HasDefaultValueSql("'0'")
        .HasColumnName("is_primary");

        entity.HasOne(d => d.Media).WithMany(p => p.InventoryTransactionMedia)
        .HasForeignKey(d => d.MediaId)
        .HasConstraintName("fk_inventory_transaction_media_asset");

        entity.HasOne(d => d.Transaction).WithMany(p => p.InventoryTransactionMedia)
        .HasForeignKey(d => d.TransactionId)
        .HasConstraintName("fk_inventory_transaction_media_transaction");
    }
}
