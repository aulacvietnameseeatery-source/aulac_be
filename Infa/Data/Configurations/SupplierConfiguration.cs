using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infa.Data.Configurations;

public sealed class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> entity)
    {
        entity.HasKey(e => e.SupplierId).HasName("PRIMARY");

        entity.ToTable("supplier");

        entity.Property(e => e.SupplierId).HasColumnName("supplier_id");
        entity.Property(e => e.Email)
        .HasMaxLength(150)
        .HasColumnName("email");
        entity.Property(e => e.Phone)
        .HasMaxLength(50)
        .HasColumnName("phone");
        entity.Property(e => e.SupplierName)
        .HasMaxLength(200)
        .HasColumnName("supplier_name");
        entity.Property(e => e.Address)
        .HasMaxLength(500)
        .HasColumnName("address");
        entity.Property(e => e.TaxCode)
        .HasMaxLength(50)
        .HasColumnName("tax_code");
    }
}
