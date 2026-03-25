using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infa.Data.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> entity)
    {
        entity.HasKey(e => e.OrderId).HasName("PRIMARY");

        entity.ToTable("orders");

        entity.HasIndex(e => e.CustomerId, "FK_orders_customer_id");

        entity.HasIndex(e => e.CreatedAt, "idx_order_status");

        entity.HasIndex(e => e.SourceLvId, "idx_orders_source_lv");

        entity.HasIndex(e => e.OrderStatusLvId, "idx_orders_status_lv");

        entity.HasIndex(e => e.StaffId, "orders_ibfk_2");

        entity.HasIndex(e => e.TableId, "table_id");

        entity.Property(e => e.OrderId).HasColumnName("order_id");
        entity.Property(e => e.CreatedAt)
        .HasDefaultValueSql("CURRENT_TIMESTAMP")
        .HasColumnType("datetime")
        .HasColumnName("created_at");
        entity.Property(e => e.CustomerId).HasColumnName("customer_id");
        entity.Property(e => e.OrderStatusLvId).HasColumnName("order_status_lv_id");
        entity.Property(e => e.SourceLvId).HasColumnName("source_lv_id");
        entity.Property(e => e.StaffId).HasColumnName("staff_id");
        entity.Property(e => e.TableId).HasColumnName("table_id").IsRequired(false);
        entity.Property(e => e.TipAmount)
        .HasPrecision(14, 2)
        .HasColumnName("tip_amount");
        entity.Property(e => e.TotalAmount)
        .HasPrecision(14, 2)
        .HasColumnName("total_amount");
        entity.Property(e => e.SubTotalAmount)
        .HasPrecision(14, 2)
        .HasColumnName("sub_total_amount")
        .HasDefaultValue(0);
        entity.Property(e => e.UpdatedAt)
        .HasColumnType("datetime")
        .HasColumnName("updated_at");

        entity.Property(e => e.TaxId).HasColumnName("tax_id");
        entity.Property(e => e.TaxAmount)
        .HasPrecision(14, 2)
        .HasColumnName("tax_amount")
        .HasDefaultValue(0);

        entity.HasOne(d => d.Customer).WithMany(p => p.Orders)
        .HasForeignKey(d => d.CustomerId)
        .OnDelete(DeleteBehavior.ClientSetNull)
        .HasConstraintName("FK_orders_customer_id");

        entity.HasOne(d => d.OrderStatusLv).WithMany(p => p.OrderOrderStatusLvs)
        .HasForeignKey(d => d.OrderStatusLvId)
        .OnDelete(DeleteBehavior.ClientSetNull)
        .HasConstraintName("fk_orders_status_lv");

        entity.HasOne(d => d.SourceLv).WithMany(p => p.OrderSourceLvs)
        .HasForeignKey(d => d.SourceLvId)
        .OnDelete(DeleteBehavior.ClientSetNull)
        .HasConstraintName("fk_orders_source_lv");

        entity.HasOne(d => d.Staff).WithMany(p => p.Orders)
        .HasForeignKey(d => d.StaffId)
        .OnDelete(DeleteBehavior.ClientSetNull)
        .HasConstraintName("orders_ibfk_2");

        entity.HasOne(d => d.Table).WithMany(p => p.Orders)
        .HasForeignKey(d => d.TableId)
        .OnDelete(DeleteBehavior.ClientSetNull)
        .HasConstraintName("orders_ibfk_1");

        entity.HasOne(d => d.Tax).WithMany(p => p.Orders)
        .HasForeignKey(d => d.TaxId)
        .OnDelete(DeleteBehavior.SetNull)
        .HasConstraintName("fk_orders_tax");
    }
}

