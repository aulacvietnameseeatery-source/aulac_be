using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infa.Data.Configurations;

public sealed class LookupValueConfiguration : IEntityTypeConfiguration<LookupValue>
{
    public void Configure(EntityTypeBuilder<LookupValue> entity)
    {
        entity.HasKey(e => e.ValueId).HasName("PRIMARY");

        entity.ToTable("lookup_value");

        entity.HasIndex(e => e.ValueDescTextId, "fk_lookup_value_desc_text");

        entity.HasIndex(e => e.ValueNameTextId, "fk_lookup_value_name_text");

        entity.HasIndex(e => new { e.TypeId, e.IsActive, e.SortOrder }, "idx_lookup_value_type_active");

        entity.HasIndex(e => new { e.TypeId, e.ValueCode }, "uq_lookup_value").IsUnique();

        entity.Property(e => e.ValueId).HasColumnName("value_id");
        entity.Property(e => e.DeletedAt)
        .HasComment("Soft delete timestamp; never hard delete lookup values")
        .HasColumnType("datetime")
        .HasColumnName("deleted_at");
        entity.Property(e => e.Description)
        .HasColumnType("text")
        .HasColumnName("description");
        entity.Property(e => e.IsActive)
        .IsRequired()
        .HasDefaultValueSql("'1'")
        .HasColumnName("is_active");
        entity.Property(e => e.IsSystem)
        .IsRequired()
        .HasDefaultValueSql("'1'")
        .HasComment("1 = system/seeded value,0 = user-added value")
        .HasColumnName("is_system");
        entity.Property(e => e.Locked)
        .IsRequired()
        .HasDefaultValueSql("'1'")
        .HasComment("1 = value_code cannot be changed and value cannot be deleted")
        .HasColumnName("locked");
        entity.Property(e => e.Meta)
        .HasColumnType("json")
        .HasColumnName("meta");
        entity.Property(e => e.SortOrder).HasColumnName("sort_order");
        entity.Property(e => e.TypeId).HasColumnName("type_id");
        entity.Property(e => e.UpdateAt)
        .HasColumnType("datetime")
        .HasColumnName("update_at");
        entity.Property(e => e.ValueCode)
        .HasMaxLength(50)
        .HasColumnName("value_code");
        entity.Property(e => e.ValueDescTextId).HasColumnName("value_desc_text_id");
        entity.Property(e => e.ValueName)
        .HasMaxLength(150)
        .HasColumnName("value_name");
        entity.Property(e => e.ValueNameTextId).HasColumnName("value_name_text_id");

        entity.HasOne(d => d.Type).WithMany(p => p.LookupValues)
        .HasForeignKey(d => d.TypeId)
        .OnDelete(DeleteBehavior.ClientSetNull)
        .HasConstraintName("fk_lookup_value_type");

        entity.HasOne(d => d.ValueDescText).WithMany(p => p.LookupValueValueDescTexts)
        .HasForeignKey(d => d.ValueDescTextId)
        .HasConstraintName("fk_lookup_value_desc_text");

        entity.HasOne(d => d.ValueNameText).WithMany(p => p.LookupValueValueNameTexts)
        .HasForeignKey(d => d.ValueNameTextId)
        .HasConstraintName("fk_lookup_value_name_text");
    }
}
