using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infa.Data.Configurations;

public sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> entity)
    {
        entity.HasKey(e => e.PermissionId).HasName("PRIMARY");

        entity.ToTable("permission");

        entity.Property(e => e.PermissionId).HasColumnName("permission_id");
        entity.Property(e => e.ActionCode)
        .HasMaxLength(20)
        .HasColumnName("action_code");
        entity.Property(e => e.ScreenCode)
        .HasMaxLength(100)
        .HasColumnName("screen_code");
    }
}
