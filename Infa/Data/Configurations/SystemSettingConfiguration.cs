using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infa.Data.Configurations;

public sealed class SystemSettingConfiguration : IEntityTypeConfiguration<SystemSetting>
{
    public void Configure(EntityTypeBuilder<SystemSetting> entity)
    {
        entity.HasKey(e => e.SettingId).HasName("PRIMARY");

        entity.ToTable("system_setting");

        entity.HasIndex(e => e.UpdatedBy, "fk_setting_updated_by");

        entity.HasIndex(e => e.SettingKey, "uq_setting_key").IsUnique();

        entity.Property(e => e.SettingId).HasColumnName("setting_id");
        entity.Property(e => e.Description)
        .HasMaxLength(255)
        .HasColumnName("description");
        entity.Property(e => e.IsSensitive).HasColumnName("is_sensitive");
        entity.Property(e => e.SettingKey)
        .HasMaxLength(100)
        .HasColumnName("setting_key");
        entity.Property(e => e.UpdatedAt)
        .ValueGeneratedOnAddOrUpdate()
        .HasDefaultValueSql("CURRENT_TIMESTAMP")
        .HasColumnType("datetime")
        .HasColumnName("updated_at");
        entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");
        entity.Property(e => e.ValueBool).HasColumnName("value_bool");
        entity.Property(e => e.ValueDecimal)
        .HasPrecision(18, 6)
        .HasColumnName("value_decimal");
        entity.Property(e => e.ValueInt).HasColumnName("value_int");
        entity.Property(e => e.ValueJson)
        .HasColumnType("json")
        .HasColumnName("value_json");
        entity.Property(e => e.ValueString)
        .HasMaxLength(500)
        .HasColumnName("value_string");
        entity.Property(e => e.ValueType)
        .HasColumnType("enum('STRING','INT','DECIMAL','BOOL','JSON','DATETIME')")
        .HasColumnName("value_type");

        entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.SystemSettings)
        .HasForeignKey(d => d.UpdatedBy)
        .HasConstraintName("fk_setting_updated_by");
    }
}
