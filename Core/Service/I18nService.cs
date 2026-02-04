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
            if (!translations.ContainsKey(sourceLang))
                throw new InvalidOperationException("Source language missing");

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

            await _repo.AddAsync(text, ct);
            return text.TextId;
        }
    }
}
