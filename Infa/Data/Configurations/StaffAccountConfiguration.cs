using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infa.Data.Configurations;

public sealed class StaffAccountConfiguration : IEntityTypeConfiguration<StaffAccount>
{
    public void Configure(EntityTypeBuilder<StaffAccount> entity)
    {
        entity.HasKey(e => e.AccountId).HasName("PRIMARY");

        entity.ToTable("staff_account");

        entity.HasIndex(e => e.Email, "email").IsUnique();

        entity.HasIndex(e => e.AccountStatusLvId, "idx_staff_account_status_lv");

        entity.HasIndex(e => e.RoleId, "role_id");

        entity.HasIndex(e => e.Username, "username").IsUnique();

        entity.Property(e => e.AccountId).HasColumnName("account_id");
        entity.Property(e => e.AccountStatusLvId).HasColumnName("account_status_lv_id");
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
        entity.Property(e => e.IsLocked).HasColumnName("is_locked");
        entity.Property(e => e.LastLoginAt)
        .HasColumnType("datetime")
        .HasColumnName("last_login_at");
        entity.Property(e => e.PasswordHash)
        .HasMaxLength(255)
        .HasColumnName("password_hash");
        entity.Property(e => e.Phone)
        .HasMaxLength(30)
        .HasColumnName("phone");
        entity.Property(e => e.RoleId).HasColumnName("role_id");
        entity.Property(e => e.Username)
        .HasMaxLength(100)
        .HasColumnName("username");

        entity.HasOne(d => d.Role).WithMany(p => p.StaffAccounts)
        .HasForeignKey(d => d.RoleId)
        .OnDelete(DeleteBehavior.ClientSetNull)
        .HasConstraintName("staff_account_ibfk_1");
    }
}
