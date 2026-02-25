using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infa.Data.Configurations;

public sealed class TableMediumConfiguration : IEntityTypeConfiguration<TableMedium>
{
    public void Configure(EntityTypeBuilder<TableMedium> entity)
    {
        entity.HasKey(e => new { e.TableId, e.MediaId })
        .HasName("PRIMARY")
        .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });

        entity.ToTable("table_media");

        entity.HasIndex(e => e.MediaId, "media_id");

        entity.Property(e => e.TableId).HasColumnName("table_id");
        entity.Property(e => e.MediaId).HasColumnName("media_id");
        entity.Property(e => e.IsPrimary)
        .HasDefaultValueSql("'0'")
        .HasColumnName("is_primary");

        entity.HasOne(d => d.Media).WithMany(p => p.TableMedia)
        .HasForeignKey(d => d.MediaId)
        .OnDelete(DeleteBehavior.ClientSetNull)
        .HasConstraintName("table_media_ibfk_2");

        entity.HasOne(d => d.Table).WithMany(p => p.TableMedia)
        .HasForeignKey(d => d.TableId)
        .OnDelete(DeleteBehavior.ClientSetNull)
        .HasConstraintName("table_media_ibfk_1");
    }
}
