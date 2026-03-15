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
        entity.Property(e => e.ShiftTemplateId).HasColumnName("shift_template_id");
        entity.Property(e => e.StaffId).HasColumnName("staff_id");

        entity.Property(e => e.WorkDate)
            .HasColumnName("work_date");

        entity.Property(e => e.PlannedStartAt)
            .HasColumnType("datetime")
            .HasColumnName("planned_start_at");

        entity.Property(e => e.PlannedEndAt)
            .HasColumnType("datetime")
            .HasColumnName("planned_end_at");

        entity.Property(e => e.IsActive)
            .HasDefaultValue(true)
            .HasColumnName("is_active");

        entity.Property(e => e.Notes)
            .HasMaxLength(500)
            .HasColumnName("notes");

        entity.Property(e => e.AssignedBy).HasColumnName("assigned_by");

        entity.Property(e => e.AssignedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .HasColumnType("datetime")
            .HasColumnName("assigned_at");

        entity.Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .HasColumnType("datetime")
            .HasColumnName("created_at");

        entity.Property(e => e.UpdatedAt)
            .ValueGeneratedOnAddOrUpdate()
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .HasColumnType("datetime")
            .HasColumnName("updated_at");

        // Indexes
        entity.HasIndex(e => e.ShiftTemplateId, "idx_shift_assignment_template");
        entity.HasIndex(e => e.StaffId, "idx_shift_assignment_staff");
        entity.HasIndex(e => e.WorkDate, "idx_shift_assignment_work_date");
        entity.HasIndex(e => new { e.ShiftTemplateId, e.WorkDate, e.StaffId }, "uq_shift_assignment_template_date_staff").IsUnique();

        // Relationships
        entity.HasOne(e => e.ShiftTemplate)
            .WithMany(t => t.ShiftAssignments)
            .HasForeignKey(e => e.ShiftTemplateId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_shift_assignment_template");

        entity.HasOne(e => e.Staff)
            .WithMany()
            .HasForeignKey(e => e.StaffId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("fk_shift_assignment_staff");

        entity.HasOne(e => e.AssignedByStaff)
            .WithMany()
            .HasForeignKey(e => e.AssignedBy)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("fk_shift_assignment_assigned_by");
    }
}
