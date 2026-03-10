using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infa.Data.Configurations;

public sealed class ShiftAssignmentConfiguration : IEntityTypeConfiguration<ShiftAssignment>
{
    public void Configure(EntityTypeBuilder<ShiftAssignment> entity)
    {
        entity.HasKey(e => e.ShiftAssignmentId).HasName("PRIMARY");
        entity.ToTable("shift_assignment");

        entity.Property(e => e.ShiftAssignmentId).HasColumnName("shift_assignment_id");
        entity.Property(e => e.ShiftScheduleId).HasColumnName("shift_schedule_id");
        entity.Property(e => e.StaffId).HasColumnName("staff_id");
        entity.Property(e => e.RoleId).HasColumnName("role_id");
        entity.Property(e => e.AssignmentStatusLvId).HasColumnName("assignment_status_lv_id");
        entity.Property(e => e.AssignedBy).HasColumnName("assigned_by");

        entity.Property(e => e.AssignedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .HasColumnType("datetime")
            .HasColumnName("assigned_at");

        entity.Property(e => e.Remarks)
            .HasMaxLength(500)
              .HasColumnName("remarks");

        // ?? Indexes ??
        entity.HasIndex(e => e.ShiftScheduleId, "idx_shift_assignment_schedule");
        entity.HasIndex(e => e.StaffId, "idx_shift_assignment_staff");
        entity.HasIndex(e => new { e.ShiftScheduleId, e.StaffId }, "uq_shift_assignment_schedule_staff").IsUnique();

        // ?? Relationships ??
        entity.HasOne(e => e.ShiftSchedule)
            .WithMany(s => s.ShiftAssignments)
            .HasForeignKey(e => e.ShiftScheduleId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_shift_assignment_schedule");

        entity.HasOne(e => e.Staff)
            .WithMany()
            .HasForeignKey(e => e.StaffId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("fk_shift_assignment_staff");

        entity.HasOne(e => e.Role)
            .WithMany()
            .HasForeignKey(e => e.RoleId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("fk_shift_assignment_role");

        entity.HasOne(e => e.AssignmentStatusLv)
            .WithMany()
            .HasForeignKey(e => e.AssignmentStatusLvId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("fk_shift_assignment_status_lv");

        entity.HasOne(e => e.AssignedByStaff)
            .WithMany()
            .HasForeignKey(e => e.AssignedBy)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("fk_shift_assignment_assigned_by");
    }
}
