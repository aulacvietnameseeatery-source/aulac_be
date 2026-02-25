using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infa.Data.Configurations;

public sealed class I18nTextConfiguration : IEntityTypeConfiguration<I18nText>
{
    public void Configure(EntityTypeBuilder<I18nText> entity)
    {
        entity.HasKey(e => e.TextId).HasName("PRIMARY");

        entity.ToTable("i18n_text");

        entity.HasIndex(e => e.SourceLangCode, "idx_i18n_text_source_lang");

        entity.HasIndex(e => e.TextKey, "uq_i18n_text_key").IsUnique();

        entity.Property(e => e.TextId).HasColumnName("text_id");
        entity.Property(e => e.Context)
        .HasMaxLength(255)
        .HasColumnName("context");
        entity.Property(e => e.CreatedAt)
        .HasDefaultValueSql("CURRENT_TIMESTAMP")
        .HasColumnType("datetime")
        .HasColumnName("created_at");
        entity.Property(e => e.SourceLangCode)
        .HasMaxLength(10)
        .HasDefaultValueSql("'en'")
        .HasColumnName("source_lang_code");
        entity.Property(e => e.SourceText)
        .HasColumnType("text")
        .HasColumnName("source_text");
        entity.Property(e => e.TextKey)
        .HasMaxLength(200)
        .HasColumnName("text_key");
        entity.Property(e => e.UpdatedAt)
        .ValueGeneratedOnAddOrUpdate()
        .HasDefaultValueSql("CURRENT_TIMESTAMP")
        .HasColumnType("datetime")
        .HasColumnName("updated_at");

        entity.HasOne(d => d.SourceLangCodeNavigation).WithMany(p => p.I18nTexts)
        .HasForeignKey(d => d.SourceLangCode)
        .OnDelete(DeleteBehavior.ClientSetNull)
        .HasConstraintName("fk_i18n_text_lang");
    }
}
