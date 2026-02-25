using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infa.Data.Configurations;

public sealed class ServiceErrorConfiguration : IEntityTypeConfiguration<ServiceError>
{
    public void Configure(EntityTypeBuilder<ServiceError> entity)
    {
        entity.HasKey(e => e.ErrorId).HasName("PRIMARY");

        entity.ToTable("service_error");

        entity.HasIndex(e => e.CategoryId, "category_id");

        entity.HasIndex(e => e.OrderId, "idx_service_error_order");

        entity.HasIndex(e => e.SeverityLvId, "idx_service_error_severity_lv");

        entity.HasIndex(e => new { e.StaffId, e.CreatedAt }, "idx_service_error_staff");

        entity.HasIndex(e => e.OrderItemId, "order_item_id");

        entity.HasIndex(e => e.ResolvedBy, "service_error_ibfk_7");

        entity.HasIndex(e => e.TableId, "table_id");

        entity.Property(e => e.ErrorId).HasColumnName("error_id");
        entity.Property(e => e.CategoryId).HasColumnName("category_id");
        entity.Property(e => e.CreatedAt)
        .HasDefaultValueSql("CURRENT_TIMESTAMP")
        .HasColumnType("datetime")
        .HasColumnName("created_at");
        entity.Property(e => e.Description)
        .HasMaxLength(500)
        .HasColumnName("description");
        entity.Property(e => e.IsResolved)
        .HasDefaultValueSql("'0'")
        .HasColumnName("is_resolved");
        entity.Property(e => e.OrderId).HasColumnName("order_id");
        entity.Property(e => e.OrderItemId).HasColumnName("order_item_id");
        entity.Property(e => e.PenaltyAmount)
        .HasPrecision(12, 2)
        .HasDefaultValueSql("'0.00'")
        .HasColumnName("penalty_amount");
        entity.Property(e => e.ResolvedAt)
        .HasColumnType("datetime")
        .HasColumnName("resolved_at");
        entity.Property(e => e.ResolvedBy).HasColumnName("resolved_by");
        entity.Property(e => e.SeverityLvId).HasColumnName("severity_lv_id");
        entity.Property(e => e.StaffId).HasColumnName("staff_id");
        entity.Property(e => e.TableId).HasColumnName("table_id");

        entity.HasOne(d => d.Category).WithMany(p => p.ServiceErrors)
        .HasForeignKey(d => d.CategoryId)
        .OnDelete(DeleteBehavior.ClientSetNull)
        .HasConstraintName("service_error_ibfk_5");

        entity.HasOne(d => d.Order).WithMany(p => p.ServiceErrors)
        .HasForeignKey(d => d.OrderId)
        .HasConstraintName("service_error_ibfk_2");

        entity.HasOne(d => d.OrderItem).WithMany(p => p.ServiceErrors)
        .HasForeignKey(d => d.OrderItemId)
        .HasConstraintName("service_error_ibfk_3");

        entity.HasOne(d => d.ResolvedByNavigation).WithMany(p => p.ServiceErrorResolvedByNavigations)
        .HasForeignKey(d => d.ResolvedBy)
        .HasConstraintName("service_error_ibfk_7");

        entity.HasOne(d => d.SeverityLv).WithMany(p => p.ServiceErrors)
        .HasForeignKey(d => d.SeverityLvId)
        .OnDelete(DeleteBehavior.ClientSetNull)
        .HasConstraintName("fk_service_error_severity_lv");

        entity.HasOne(d => d.Staff).WithMany(p => p.ServiceErrorStaffs)
        .HasForeignKey(d => d.StaffId)
        .OnDelete(DeleteBehavior.ClientSetNull)
        .HasConstraintName("service_error_ibfk_1");

        entity.HasOne(d => d.Table).WithMany(p => p.ServiceErrors)
        .HasForeignKey(d => d.TableId)
        .HasConstraintName("service_error_ibfk_4");
    }
}
