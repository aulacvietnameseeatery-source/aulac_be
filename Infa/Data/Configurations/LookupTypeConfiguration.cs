using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infa.Data.Configurations;

public sealed class LookupTypeConfiguration : IEntityTypeConfiguration<LookupType>
{
    public void Configure(EntityTypeBuilder<LookupType> entity)
    {
        entity.HasKey(e => e.TypeId).HasName("PRIMARY");

        entity.ToTable("lookup_type");

        entity.HasIndex(e => e.TypeDescTextId, "fk_lookup_type_desc_text");

        entity.HasIndex(e => e.TypeNameTextId, "fk_lookup_type_name_text");

        entity.HasIndex(e => e.TypeCode, "uq_lookup_type_code").IsUnique();

        entity.Property(e => e.TypeId).HasColumnName("type_id");
        entity.Property(e => e.Description)
        .HasMaxLength(255)
        .HasColumnName("description");
        entity.Property(e => e.IsConfigurable)
        .HasComment("1 = admin can add/remove values,0 = controlled enum (statuses, workflows)")
        .HasColumnName("is_configurable");
        entity.Property(e => e.IsSystem)
        .IsRequired()
        .HasDefaultValueSql("'1'")
        .HasComment("1 = system-defined enum type,0 = user-defined/custom type")
        .HasColumnName("is_system");
        entity.Property(e => e.TypeCode)
        .HasMaxLength(50)
        .HasColumnName("type_code");
        entity.Property(e => e.TypeDescTextId).HasColumnName("type_desc_text_id");
        entity.Property(e => e.TypeName)
        .HasMaxLength(150)
        .HasColumnName("type_name");
        entity.Property(e => e.TypeNameTextId).HasColumnName("type_name_text_id");

        entity.HasOne(d => d.TypeDescText).WithMany(p => p.LookupTypeTypeDescTexts)
        .HasForeignKey(d => d.TypeDescTextId)
        .HasConstraintName("fk_lookup_type_desc_text");

        entity.HasOne(d => d.TypeNameText).WithMany(p => p.LookupTypeTypeNameTexts)
        .HasForeignKey(d => d.TypeNameTextId)
        .HasConstraintName("fk_lookup_type_name_text");
    }
}
