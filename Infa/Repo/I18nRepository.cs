using Core.Entity;
using Core.Interface.Repo;
using Infa.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infa.Repo
{
    public class I18nRepository : II18nRepository
    {
        private readonly RestaurantMgmtContext _db;

        public I18nRepository(RestaurantMgmtContext db)
        {
            _db = db;
        }

        public async Task AddAsync(I18nText text, CancellationToken ct)
        {
            _db.I18nTexts.Add(text);
            await _db.SaveChangesAsync(ct);
        }

        public async Task<I18nText> CreateTextAsync(
            string sourceLang,
            string sourceText,
            string context,
            CancellationToken ct)
        {
            var text = new I18nText
            {
                TextKey = Guid.NewGuid().ToString(),
                SourceLangCode = sourceLang,
                SourceText = sourceText,
                Context = context,
                CreatedAt = DateTime.UtcNow
            };

            _db.I18nTexts.Add(text);
            await _db.SaveChangesAsync(ct);

            return text;
        }

        public async Task AddTranslationsAsync(
            long textId,
            Dictionary<string, string> translations,
            string sourceLang,
            CancellationToken ct)
        {
            foreach (var (lang, value) in translations)
            {
                if (lang == sourceLang) continue;

                _db.I18nTranslations.Add(new I18nTranslation
                {
                    TextId = textId,
                    LangCode = lang,
                    TranslatedText = value,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync(ct);
        }
    }

}
