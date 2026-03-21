using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infa.Data.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> entity)
    {
        entity.HasKey(e => e.NotificationId).HasName("PRIMARY");

        entity.ToTable("notifications");

        entity.HasIndex(e => e.CreatedAt, "idx_notifications_created_at");
        entity.HasIndex(e => e.Type, "idx_notifications_type");

        entity.Property(e => e.NotificationId).HasColumnName("notification_id");

        entity.Property(e => e.Type)
            .HasMaxLength(50)
            .HasColumnName("type");

        entity.Property(e => e.Title)
            .HasMaxLength(200)
            .HasColumnName("title");

        entity.Property(e => e.Body)
            .HasMaxLength(1000)
            .HasColumnName("body");

        entity.Property(e => e.Priority)
            .HasMaxLength(20)
            .HasColumnName("priority");

        entity.Property(e => e.RequireAck)
            .HasColumnName("require_ack");

        entity.Property(e => e.SoundKey)
            .HasMaxLength(100)
            .HasColumnName("sound_key");

        entity.Property(e => e.ActionUrl)
            .HasMaxLength(500)
            .HasColumnName("action_url");

        entity.Property(e => e.EntityType)
            .HasMaxLength(50)
            .HasColumnName("entity_type");

        entity.Property(e => e.EntityId)
            .HasMaxLength(50)
            .HasColumnName("entity_id");

        entity.Property(e => e.MetadataJson)
            .HasColumnType("json")
            .HasColumnName("metadata_json");

        entity.Property(e => e.TargetPermissions)
            .HasMaxLength(500)
            .HasColumnName("target_permissions");

        entity.Property(e => e.TargetUserIds)
            .HasMaxLength(500)
            .HasColumnName("target_user_ids");

        entity.Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .HasColumnType("datetime")
            .HasColumnName("created_at");
    }
}
