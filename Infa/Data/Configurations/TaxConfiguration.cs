using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infa.Data.Configurations;

public sealed class TaxConfiguration : IEntityTypeConfiguration<Tax>
{
    public void Configure(EntityTypeBuilder<Tax> entity)
    {
        entity.HasKey(e => e.TaxId).HasName("PRIMARY");

        entity.ToTable("tax");

        entity.Property(e => e.TaxId).HasColumnName("tax_id");
        
        entity.Property(e => e.TaxName)
            .HasMaxLength(100)
            .HasColumnName("tax_name");

        entity.Property(e => e.TaxRate)
            .HasPrecision(5, 2)
            .HasColumnName("tax_rate");

        entity.Property(e => e.TaxType)
            .HasMaxLength(50)
            .HasColumnName("tax_type")
            .HasDefaultValue("EXCLUSIVE");

        entity.Property(e => e.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        entity.Property(e => e.IsDefault)
            .HasColumnName("is_default")
            .HasDefaultValue(false);

        entity.Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .HasColumnType("datetime")
            .HasColumnName("created_at");

        entity.Property(e => e.UpdatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAddOrUpdate()
            .HasColumnType("datetime")
            .HasColumnName("updated_at");
    }
}
