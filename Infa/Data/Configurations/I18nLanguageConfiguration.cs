using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infa.Data.Configurations;

public sealed class I18nLanguageConfiguration : IEntityTypeConfiguration<I18nLanguage>
{
    public void Configure(EntityTypeBuilder<I18nLanguage> entity)
    {
        entity.HasKey(e => e.LangCode).HasName("PRIMARY");

        entity.ToTable("i18n_language");

        entity.Property(e => e.LangCode)
        .HasMaxLength(10)
        .HasColumnName("lang_code");
        entity.Property(e => e.IsActive)
        .IsRequired()
        .HasDefaultValueSql("'1'")
        .HasColumnName("is_active");
        entity.Property(e => e.LangName)
        .HasMaxLength(50)
        .HasColumnName("lang_name");
    }
}
