using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infa.Data.Configurations;

public sealed class NotificationPreferenceConfiguration : IEntityTypeConfiguration<NotificationPreference>
{
    public void Configure(EntityTypeBuilder<NotificationPreference> entity)
    {
        entity.HasKey(e => e.NotificationPreferenceId).HasName("PRIMARY");

        entity.ToTable("notification_preferences");

        entity.HasIndex(e => new { e.UserId, e.NotificationType }, "uq_notification_pref_user_type")
            .IsUnique();

        entity.HasIndex(e => e.UserId, "idx_notification_pref_user_id");

        entity.Property(e => e.NotificationPreferenceId)
            .HasColumnName("notification_preference_id");

        entity.Property(e => e.UserId)
            .HasColumnName("user_id");

        entity.Property(e => e.NotificationType)
            .HasMaxLength(50)
            .HasColumnName("notification_type");

        entity.Property(e => e.IsEnabled)
            .HasDefaultValue(true)
            .HasColumnName("is_enabled");

        entity.Property(e => e.SoundEnabled)
            .HasDefaultValue(true)
            .HasColumnName("sound_enabled");

        entity.Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .HasColumnType("datetime")
            .HasColumnName("created_at");

        entity.Property(e => e.UpdatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .HasColumnType("datetime")
            .HasColumnName("updated_at");
    }
}
