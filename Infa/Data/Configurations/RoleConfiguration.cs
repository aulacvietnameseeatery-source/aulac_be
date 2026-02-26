using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infa.Data.Configurations;

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> entity)
    {
        entity.HasKey(e => e.RoleId).HasName("PRIMARY");

        entity.ToTable("role");

        entity.HasIndex(e => e.RoleCode, "role_code").IsUnique();

        entity.Property(e => e.RoleId).HasColumnName("role_id");
        entity.Property(e => e.RoleCode)
        .HasMaxLength(50)
        .HasColumnName("role_code");
        entity.Property(e => e.RoleName)
        .HasMaxLength(100)
        .HasColumnName("role_name");
        entity.Property(e => e.RoleStatusLvId)
        .HasColumnName("role_status_lv_id");

        entity.HasOne(d => d.RoleStatusLv)
        .WithMany()
        .HasForeignKey(d => d.RoleStatusLvId);

        entity.HasMany(d => d.Permissions).WithMany(p => p.Roles)
        .UsingEntity<Dictionary<string, object>>(
        "RolePermission",
        r => r.HasOne<Permission>().WithMany()
        .HasForeignKey("PermissionId")
        .OnDelete(DeleteBehavior.ClientSetNull)
        .HasConstraintName("role_permission_ibfk_2"),
        l => l.HasOne<Role>().WithMany()
        .HasForeignKey("RoleId")
        .OnDelete(DeleteBehavior.ClientSetNull)
        .HasConstraintName("role_permission_ibfk_1"),
        j =>
        {
            j.HasKey("RoleId", "PermissionId")
     .HasName("PRIMARY")
     .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });
            j.ToTable("role_permission");
            j.HasIndex(new[] { "PermissionId" }, "permission_id");
            j.IndexerProperty<long>("RoleId").HasColumnName("role_id");
            j.IndexerProperty<long>("PermissionId").HasColumnName("permission_id");
        });
    }
}
