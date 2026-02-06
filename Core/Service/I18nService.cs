using Core.DTO.Dish;
using Core.Entity;
using Core.Interface.Repo;
using Core.Interface.Service.I18n;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Service
{
    public class I18nService : Ii18nService
    {
        private readonly II18nRepository _repo;

        public I18nService(II18nRepository repo)
        {
            _repo = repo;
        }

        public async Task<long> CreateAsync(string textKey, string context, string sourceLang, Dictionary<string, string> translations, CancellationToken ct)
        {
            // Ensure the source language translation is present
            if (!translations.ContainsKey(sourceLang))
                throw new InvalidOperationException("Source language missing");

            // Create a new I18nText entity with translations
            var text = new I18nText
            {
                TextKey = textKey,
                Context = context,
                SourceLangCode = sourceLang,
                SourceText = translations[sourceLang],
                CreatedAt = DateTime.UtcNow,
                I18nTranslations = translations
                    .Select(kv => new I18nTranslation
                    {
                        LangCode = kv.Key,
                        TranslatedText = kv.Value,
                        UpdatedAt = DateTime.UtcNow
                    })
                    .ToList()
            };

            // Add the new I18nText entity to the repository
            await _repo.AddAsync(text, ct);
            // Return the generated TextId of the new I18nText
            return text.TextId;
        }

        public async Task UpdateTextAsync(long textId, Dictionary<string, DishI18nDto> i18n, Func<DishI18nDto, string?> selector, CancellationToken ct)
        {
            // Retrieve the I18nText entity with its translations from the repository
            var text = await _repo.GetTextWithTranslationsAsync(textId, ct);
            if (text == null)
                throw new InvalidOperationException($"I18nText {textId} not found");

            foreach (var (lang, content) in i18n)
            {
                // Use the selector to get the translation value for the current language
                var value = selector(content);
                if (string.IsNullOrWhiteSpace(value))
                    continue; // Skip if the value is null or whitespace

                // Try to find an existing translation for the language
                var translation = text.I18nTranslations
                    .FirstOrDefault(x => x.LangCode == lang);

                if (translation == null)
                {
                    // Add a new translation if it does not exist
                    await _repo.AddTranslationAsync(new I18nTranslation
                    {
                        TextId = textId,
                        LangCode = lang,
                        TranslatedText = value,
                        UpdatedAt = DateTime.UtcNow
                    }, ct);
                }
                else
                {
                    // Update the existing translation
                    translation.TranslatedText = value;
                    translation.UpdatedAt = DateTime.UtcNow;
                }
            }
        }
    }
}
