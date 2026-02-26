using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infa.Data.Configurations;

public sealed class MediaAssetConfiguration : IEntityTypeConfiguration<MediaAsset>
{
    public void Configure(EntityTypeBuilder<MediaAsset> entity)
    {
        entity.HasKey(e => e.MediaId).HasName("PRIMARY");

        entity.ToTable("media_asset");

        entity.HasIndex(e => e.MediaTypeLvId, "idx_media_asset_type_lv");

        entity.Property(e => e.MediaId).HasColumnName("media_id");
        entity.Property(e => e.CreatedAt)
        .HasDefaultValueSql("CURRENT_TIMESTAMP")
        .HasColumnType("datetime")
        .HasColumnName("created_at");
        entity.Property(e => e.DurationSec).HasColumnName("duration_sec");
        entity.Property(e => e.Height).HasColumnName("height");
        entity.Property(e => e.MediaTypeLvId).HasColumnName("media_type_lv_id");
        entity.Property(e => e.MimeType)
        .HasMaxLength(100)
        .HasColumnName("mime_type");
        entity.Property(e => e.Url)
        .HasMaxLength(500)
        .HasColumnName("url");
        entity.Property(e => e.Width).HasColumnName("width");

        entity.HasOne(d => d.MediaTypeLv).WithMany(p => p.MediaAssets)
        .HasForeignKey(d => d.MediaTypeLvId)
        .OnDelete(DeleteBehavior.ClientSetNull)
        .HasConstraintName("fk_media_asset_type_lv");
    }
}
