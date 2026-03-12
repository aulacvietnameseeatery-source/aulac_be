using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infa.Data.Configurations;

public sealed class LoginActivityConfiguration : IEntityTypeConfiguration<LoginActivity>
{
    public void Configure(EntityTypeBuilder<LoginActivity> entity)
    {
        entity.HasKey(e => e.LoginActivityId).HasName("PRIMARY");
        entity.ToTable("login_activity");

        entity.Property(e => e.LoginActivityId).HasColumnName("login_activity_id");
        entity.Property(e => e.StaffId).HasColumnName("staff_id");
        entity.Property(e => e.SessionId).HasColumnName("session_id");

        entity.Property(e => e.EventType)
            .HasMaxLength(50)
            .HasColumnName("event_type");

        entity.Property(e => e.DeviceInfo)
            .HasMaxLength(255)
            .HasColumnName("device_info");

        entity.Property(e => e.IpAddress)
            .HasMaxLength(64)
            .HasColumnName("ip_address");

        entity.Property(e => e.OccurredAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .HasColumnType("datetime")
            .HasColumnName("occurred_at");

        // ?? Indexes ??
        entity.HasIndex(e => e.StaffId, "idx_login_activity_staff");
        entity.HasIndex(e => e.OccurredAt, "idx_login_activity_occurred_at");

        // ?? Relationships ??
        entity.HasOne(e => e.Staff)
            .WithMany()
            .HasForeignKey(e => e.StaffId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("fk_login_activity_staff");

        entity.HasOne(e => e.Session)
            .WithMany()
            .HasForeignKey(e => e.SessionId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_login_activity_session");
    }
}
