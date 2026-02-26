using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infa.Data.Configurations;

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> entity)
    {
        entity.HasKey(e => e.PaymentId).HasName("PRIMARY");

        entity.ToTable("payment");

        entity.HasIndex(e => e.MethodLvId, "idx_payment_method_lv");

        entity.HasIndex(e => e.OrderId, "payment_ibfk_1");

        entity.Property(e => e.PaymentId).HasColumnName("payment_id");
        entity.Property(e => e.ChangeAmount)
        .HasPrecision(14, 2)
        .HasColumnName("change_amount");
        entity.Property(e => e.MethodLvId).HasColumnName("method_lv_id");
        entity.Property(e => e.OrderId).HasColumnName("order_id");
        entity.Property(e => e.PaidAt)
        .HasDefaultValueSql("CURRENT_TIMESTAMP")
        .HasColumnType("datetime")
        .HasColumnName("paid_at");
        entity.Property(e => e.ReceivedAmount)
        .HasPrecision(14, 2)
        .HasColumnName("received_amount");

        entity.HasOne(d => d.MethodLv).WithMany(p => p.Payments)
        .HasForeignKey(d => d.MethodLvId)
        .OnDelete(DeleteBehavior.ClientSetNull)
        .HasConstraintName("fk_payment_method_lv");

        entity.HasOne(d => d.Order).WithMany(p => p.Payments)
        .HasForeignKey(d => d.OrderId)
        .OnDelete(DeleteBehavior.ClientSetNull)
        .HasConstraintName("payment_ibfk_1");
    }
}
