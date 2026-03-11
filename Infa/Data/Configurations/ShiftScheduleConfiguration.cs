using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infa.Data.Configurations;

public sealed class ShiftScheduleConfiguration : IEntityTypeConfiguration<ShiftSchedule>
{
    public void Configure(EntityTypeBuilder<ShiftSchedule> entity)
    {
        entity.HasKey(e => e.ShiftScheduleId).HasName("PRIMARY");
        entity.ToTable("shift_schedule");

        entity.Property(e => e.ShiftScheduleId).HasColumnName("shift_schedule_id");

        entity.Property(e => e.BusinessDate)
            .HasColumnName("business_date");

        entity.Property(e => e.ShiftTypeLvId).HasColumnName("shift_type_lv_id");
        entity.Property(e => e.StatusLvId).HasColumnName("status_lv_id");

        entity.Property(e => e.PlannedStartAt)
            .HasColumnType("datetime")
            .HasColumnName("planned_start_at");

        entity.Property(e => e.PlannedEndAt)
            .HasColumnType("datetime")
            .HasColumnName("planned_end_at");

        entity.Property(e => e.Notes)
            .HasMaxLength(500)
            .HasColumnName("notes");

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

        // ?? Indexes ??
        entity.HasIndex(e => e.BusinessDate, "idx_shift_schedule_business_date");
        entity.HasIndex(e => e.StatusLvId, "idx_shift_schedule_status_lv");
        entity.HasIndex(e => e.ShiftTypeLvId, "idx_shift_schedule_type_lv");

        // ?? Relationships ??
        entity.HasOne(e => e.ShiftTypeLv)
            .WithMany()
            .HasForeignKey(e => e.ShiftTypeLvId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("fk_shift_schedule_type_lv");

        entity.HasOne(e => e.StatusLv)
            .WithMany()
            .HasForeignKey(e => e.StatusLvId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("fk_shift_schedule_status_lv");

        entity.HasOne(e => e.CreatedByStaff)
            .WithMany()
            .HasForeignKey(e => e.CreatedBy)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("fk_shift_schedule_created_by");

        entity.HasOne(e => e.UpdatedByStaff)
            .WithMany()
            .HasForeignKey(e => e.UpdatedBy)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("fk_shift_schedule_updated_by");
    }
}
