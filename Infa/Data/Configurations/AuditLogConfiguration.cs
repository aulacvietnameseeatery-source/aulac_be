using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infa.Data.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> entity)
    {
        entity.HasKey(e => e.LogId).HasName("PRIMARY");

        entity.ToTable("audit_log");

        entity.HasIndex(e => e.StaffId, "audit_log_ibfk_1");

        entity.HasIndex(e => e.CreatedAt, "idx_audit_time");

        entity.Property(e => e.LogId).HasColumnName("log_id");
        entity.Property(e => e.ActionCode)
        .HasMaxLength(100)
        .HasColumnName("action_code");
        entity.Property(e => e.CreatedAt)
        .HasDefaultValueSql("CURRENT_TIMESTAMP")
        .HasColumnType("datetime")
        .HasColumnName("created_at");
        entity.Property(e => e.StaffId).HasColumnName("staff_id");
        entity.Property(e => e.TargetId).HasColumnName("target_id");
        entity.Property(e => e.TargetTable)
        .HasMaxLength(100)
        .HasColumnName("target_table");

        entity.HasOne(d => d.Staff).WithMany(p => p.AuditLogs)
        .HasForeignKey(d => d.StaffId)
        .HasConstraintName("audit_log_ibfk_1");
    }
}
