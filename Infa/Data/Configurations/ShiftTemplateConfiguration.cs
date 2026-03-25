using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infa.Data.Configurations;

public sealed class ShiftTemplateConfiguration : IEntityTypeConfiguration<ShiftTemplate>
{
    public void Configure(EntityTypeBuilder<ShiftTemplate> entity)
    {
        entity.HasKey(e => e.ShiftTemplateId).HasName("PRIMARY");
        entity.ToTable("shift_template");

        entity.Property(e => e.ShiftTemplateId).HasColumnName("shift_template_id");

        entity.Property(e => e.TemplateName)
            .HasMaxLength(100)
            .HasColumnName("template_name");

        entity.Property(e => e.DefaultStartTime)
            .HasColumnType("time")
            .HasColumnName("default_start_time");

        entity.Property(e => e.DefaultEndTime)
            .HasColumnType("time")
            .HasColumnName("default_end_time");

        entity.Property(e => e.Description)
            .HasMaxLength(500)
            .HasColumnName("description");

        entity.Property(e => e.BufferBeforeMinutes)
            .HasColumnName("buffer_before_minutes");

        entity.Property(e => e.BufferAfterMinutes)
            .HasColumnName("buffer_after_minutes");

        entity.Property(e => e.IsActive)
            .HasDefaultValue(true)
            .HasColumnName("is_active");

        entity.Property(e => e.CreatedBy).HasColumnName("created_by");

        entity.Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .HasColumnType("datetime")
            .HasColumnName("created_at");

        entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");

        entity.Property(e => e.UpdatedAt)
            .ValueGeneratedOnAddOrUpdate()
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .HasColumnType("datetime")
            .HasColumnName("updated_at");

        // Indexes
        entity.HasIndex(e => e.IsActive, "idx_shift_template_active");
        entity.HasIndex(e => e.TemplateName, "uq_shift_template_name").IsUnique();

        // Relationships
        entity.HasOne(e => e.CreatedByStaff)
            .WithMany()
            .HasForeignKey(e => e.CreatedBy)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("fk_shift_template_created_by");

        entity.HasOne(e => e.UpdatedByStaff)
            .WithMany()
            .HasForeignKey(e => e.UpdatedBy)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("fk_shift_template_updated_by");
    }
}
