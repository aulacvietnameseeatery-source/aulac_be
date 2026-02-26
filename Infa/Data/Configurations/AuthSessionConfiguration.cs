using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infa.Data.Configurations;

public sealed class AuthSessionConfiguration : IEntityTypeConfiguration<AuthSession>
{
    public void Configure(EntityTypeBuilder<AuthSession> entity)
    {
        entity.HasKey(e => e.SessionId).HasName("PRIMARY");

        entity.ToTable("auth_session");

        entity.HasIndex(e => new { e.UserId, e.ExpiresAt }, "idx_session_user");

        entity.Property(e => e.SessionId).HasColumnName("session_id");
        entity.Property(e => e.CreatedAt)
        .HasDefaultValueSql("CURRENT_TIMESTAMP")
        .HasColumnType("datetime")
        .HasColumnName("created_at");
        entity.Property(e => e.DeviceInfo)
        .HasMaxLength(500)
        .HasColumnName("device_info");
        entity.Property(e => e.ExpiresAt)
        .HasColumnType("datetime")
        .HasColumnName("expires_at");
        entity.Property(e => e.IpAddress)
        .HasMaxLength(45)
        .HasColumnName("ip_address");
        entity.Property(e => e.Revoked)
        .HasDefaultValueSql("'0'")
        .HasColumnName("revoked");
        entity.Property(e => e.TokenHash)
        .HasMaxLength(255)
        .HasColumnName("token_hash");
        entity.Property(e => e.UserId).HasColumnName("user_id");

        entity.HasOne(d => d.User).WithMany(p => p.AuthSessions)
        .HasForeignKey(d => d.UserId)
        .OnDelete(DeleteBehavior.ClientSetNull)
        .HasConstraintName("auth_session_ibfk_1");
    }
}
