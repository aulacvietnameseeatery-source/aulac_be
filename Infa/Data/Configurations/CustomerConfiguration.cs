using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infa.Data.Configurations;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> entity)
    {
        entity.HasKey(e => e.CustomerId).HasName("PRIMARY");

        entity.ToTable("customer");

        entity.HasIndex(e => e.Phone, "phone").IsUnique();

        entity.Property(e => e.CustomerId).HasColumnName("customer_id");
        entity.Property(e => e.CreatedAt)
        .HasDefaultValueSql("CURRENT_TIMESTAMP")
        .HasColumnType("datetime")
        .HasColumnName("created_at");
        entity.Property(e => e.Email)
        .HasMaxLength(150)
        .HasColumnName("email");
        entity.Property(e => e.FullName)
        .HasMaxLength(150)
        .HasColumnName("full_name");
        entity.Property(e => e.IsMember)
        .HasDefaultValueSql("'0'")
        .HasColumnName("is_member");
        entity.Property(e => e.LoyaltyPoints)
        .HasDefaultValueSql("'0'")
        .HasColumnName("loyalty_points");
        entity.Property(e => e.Phone)
        .HasMaxLength(30)
        .HasColumnName("phone");
    }
}
