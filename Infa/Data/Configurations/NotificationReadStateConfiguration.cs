using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infa.Data.Configurations;

public sealed class NotificationReadStateConfiguration : IEntityTypeConfiguration<NotificationReadState>
{
    public void Configure(EntityTypeBuilder<NotificationReadState> entity)
    {
        entity.HasKey(e => e.NotificationReadStateId).HasName("PRIMARY");

        entity.ToTable("notification_read_states");

        entity.HasIndex(e => new { e.NotificationId, e.UserId }, "uq_notification_user")
            .IsUnique();

        entity.HasIndex(e => new { e.UserId, e.IsRead }, "idx_nrs_user_is_read");

        entity.HasIndex(e => e.NotificationId, "idx_nrs_notification_id");

        entity.Property(e => e.NotificationReadStateId)
            .HasColumnName("notification_read_state_id");

        entity.Property(e => e.NotificationId)
            .HasColumnName("notification_id");

        entity.Property(e => e.UserId)
            .HasColumnName("user_id");

        entity.Property(e => e.IsRead)
            .HasDefaultValue(false)
            .HasColumnName("is_read");

        entity.Property(e => e.IsAcknowledged)
            .HasDefaultValue(false)
            .HasColumnName("is_acknowledged");

        entity.Property(e => e.ReadAt)
            .HasColumnType("datetime")
            .HasColumnName("read_at");

        entity.Property(e => e.AcknowledgedAt)
            .HasColumnType("datetime")
            .HasColumnName("acknowledged_at");

        entity.Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .HasColumnType("datetime")
            .HasColumnName("created_at");

        entity.HasOne(d => d.Notification)
            .WithMany(p => p.ReadStates)
            .HasForeignKey(d => d.NotificationId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_nrs_notification");
    }
}
