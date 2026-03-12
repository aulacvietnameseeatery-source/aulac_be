using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infa.Data.Configurations;

public sealed class AttendanceRecordConfiguration : IEntityTypeConfiguration<AttendanceRecord>
{
    public void Configure(EntityTypeBuilder<AttendanceRecord> entity)
    {
        entity.HasKey(e => e.AttendanceId).HasName("PRIMARY");
        entity.ToTable("attendance_record");

        entity.Property(e => e.AttendanceId).HasColumnName("attendance_id");
        entity.Property(e => e.ShiftAssignmentId).HasColumnName("shift_assignment_id");
        entity.Property(e => e.AttendanceStatusLvId).HasColumnName("attendance_status_lv_id");

        entity.Property(e => e.ActualCheckInAt)
            .HasColumnType("datetime")
            .HasColumnName("actual_check_in_at");

        entity.Property(e => e.ActualCheckOutAt)
            .HasColumnType("datetime")
            .HasColumnName("actual_check_out_at");

        entity.Property(e => e.LateMinutes)
             .HasDefaultValue(0)
           .HasColumnName("late_minutes");

        entity.Property(e => e.EarlyLeaveMinutes)
            .HasDefaultValue(0)
            .HasColumnName("early_leave_minutes");

        entity.Property(e => e.WorkedMinutes)
            .HasDefaultValue(0)
            .HasColumnName("worked_minutes");

        entity.Property(e => e.IsManualAdjustment)
            .HasDefaultValue(false)
            .HasColumnName("is_manual_adjustment");

        entity.Property(e => e.AdjustmentReason)
            .HasMaxLength(500)
            .HasColumnName("adjustment_reason");

        entity.Property(e => e.ReviewedBy).HasColumnName("reviewed_by");

        entity.Property(e => e.ReviewedAt)
            .HasColumnType("datetime")
            .HasColumnName("reviewed_at");

        entity.Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .HasColumnType("datetime")
            .HasColumnName("created_at");

        entity.Property(e => e.UpdatedAt)
            .ValueGeneratedOnAddOrUpdate()
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .HasColumnType("datetime")
            .HasColumnName("updated_at");

        // ?? Indexes ??
        entity.HasIndex(e => e.ShiftAssignmentId, "uq_attendance_assignment").IsUnique();
        entity.HasIndex(e => e.AttendanceStatusLvId, "idx_attendance_status_lv");

        // ?? Relationships ??
        entity.HasOne(e => e.ShiftAssignment)
            .WithOne(a => a.AttendanceRecord)
            .HasForeignKey<AttendanceRecord>(e => e.ShiftAssignmentId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_attendance_assignment");

        entity.HasOne(e => e.AttendanceStatusLv)
            .WithMany()
            .HasForeignKey(e => e.AttendanceStatusLvId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("fk_attendance_status_lv");

        entity.HasOne(e => e.ReviewedByStaff)
            .WithMany()
            .HasForeignKey(e => e.ReviewedBy)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("fk_attendance_reviewed_by");
    }
}
