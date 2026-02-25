using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infa.Data.Configurations;

public sealed class I18nTranslationConfiguration : IEntityTypeConfiguration<I18nTranslation>
{
 public void Configure(EntityTypeBuilder<I18nTranslation> entity)
 {
 entity.HasKey(e => new { e.TextId, e.LangCode })
 .HasName("PRIMARY")
 .HasAnnotation("MySql:IndexPrefixLength", new[] {0,0});

 entity.ToTable("i18n_translation");

 entity.HasIndex(e => e.LangCode, "fk_i18n_tr_lang");

 entity.Property(e => e.TextId).HasColumnName("text_id");
 entity.Property(e => e.LangCode)
 .HasMaxLength(10)
 .HasColumnName("lang_code");
 entity.Property(e => e.TranslatedText)
 .HasColumnType("text")
 .HasColumnName("translated_text");
 entity.Property(e => e.UpdatedAt)
 .ValueGeneratedOnAddOrUpdate()
 .HasDefaultValueSql("CURRENT_TIMESTAMP")
 .HasColumnType("datetime")
 .HasColumnName("updated_at");

 entity.HasOne(d => d.LangCodeNavigation).WithMany(p => p.I18nTranslations)
 .HasForeignKey(d => d.LangCode)
 .OnDelete(DeleteBehavior.ClientSetNull)
 .HasConstraintName("fk_i18n_tr_lang");

 entity.HasOne(d => d.Text).WithMany(p => p.I18nTranslations)
 .HasForeignKey(d => d.TextId)
 .HasConstraintName("fk_i18n_tr_text");
 }
}
