using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infa.Data.Configurations;

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> entity)
    {
        entity.HasKey(e => e.OrderItemId).HasName("PRIMARY");

        entity.ToTable("order_item");

        entity.HasIndex(e => e.DishId, "dish_id");

        entity.HasIndex(e => e.ItemStatusLvId, "idx_order_item_status_lv");

        entity.HasIndex(e => e.OrderId, "order_id");

        entity.Property(e => e.OrderItemId).HasColumnName("order_item_id");
        entity.Property(e => e.DishId).HasColumnName("dish_id");
        entity.Property(e => e.ItemStatus)
        .HasDefaultValueSql("'1'")
        .HasComment("OrderItemStatus:1=CREATED,2=IN_PROGRESS,3=READY,4=SERVED,5=REJECTED")
        .HasColumnName("item_status");
        entity.Property(e => e.ItemStatusLvId).HasColumnName("item_status_lv_id");
        entity.Property(e => e.OrderId).HasColumnName("order_id");
        entity.Property(e => e.Price)
        .HasPrecision(12, 2)
        .HasColumnName("price");
        entity.Property(e => e.Quantity).HasColumnName("quantity");
        entity.Property(e => e.RejectReason)
        .HasMaxLength(255)
        .HasColumnName("reject_reason");

        entity.HasOne(d => d.Dish).WithMany(p => p.OrderItems)
        .HasForeignKey(d => d.DishId)
        .OnDelete(DeleteBehavior.ClientSetNull)
        .HasConstraintName("order_item_ibfk_2");

        entity.HasOne(d => d.ItemStatusLv).WithMany(p => p.OrderItems)
        .HasForeignKey(d => d.ItemStatusLvId)
        .OnDelete(DeleteBehavior.ClientSetNull)
        .HasConstraintName("fk_order_item_status_lv");

        entity.HasOne(d => d.Order).WithMany(p => p.OrderItems)
        .HasForeignKey(d => d.OrderId)
        .OnDelete(DeleteBehavior.ClientSetNull)
        .HasConstraintName("order_item_ibfk_1");
    }
}
