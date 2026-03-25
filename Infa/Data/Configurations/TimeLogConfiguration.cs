using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infa.Data.Configurations;

public sealed class TimeLogConfiguration : IEntityTypeConfiguration<TimeLog>
{
    public void Configure(EntityTypeBuilder<TimeLog> entity)
    {
        entity.HasKey(e => e.TimeLogId).HasName("PRIMARY");
        entity.ToTable("time_log");

        entity.Property(e => e.TimeLogId).HasColumnName("time_log_id");
        entity.Property(e => e.AttendanceRecordId).HasColumnName("attendance_record_id");

        entity.Property(e => e.PunchInTime)
            .HasColumnType("datetime")
            .HasColumnName("punch_in_time");

        entity.Property(e => e.PunchOutTime)
            .HasColumnType("datetime")
            .HasColumnName("punch_out_time");

        entity.Property(e => e.GpsLocationIn)
            .HasMaxLength(50)
            .HasColumnName("gps_location_in");

        entity.Property(e => e.GpsLocationOut)
            .HasMaxLength(50)
            .HasColumnName("gps_location_out");

        entity.Property(e => e.DeviceIdIn)
            .HasMaxLength(255)
            .HasColumnName("device_id_in");

        entity.Property(e => e.DeviceIdOut)
            .HasMaxLength(255)
            .HasColumnName("device_id_out");

        entity.Property(e => e.ValidationStatus)
            .HasMaxLength(30)
            .HasDefaultValue("Valid")
            .HasColumnName("validation_status");

        entity.Property(e => e.PunchDurationMinutes)
            .HasDefaultValue(0)
            .HasColumnName("punch_duration_minutes");

        entity.Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .HasColumnType("datetime")
            .HasColumnName("created_at");

        // Indexes
        entity.HasIndex(e => e.AttendanceRecordId, "idx_time_log_attendance");

        // Relationships
        entity.HasOne(e => e.AttendanceRecord)
            .WithMany(a => a.TimeLogs)
            .HasForeignKey(e => e.AttendanceRecordId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_time_log_attendance");
    }
}
