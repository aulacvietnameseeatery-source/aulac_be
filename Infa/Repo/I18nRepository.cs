using Core.Entity;
using Core.Interface.Repo;
using Infa.Data;
using Microsoft.EntityFrameworkCore;
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
            _db.I18nTexts.Add(text); // Add new I18nText entity to the context
            await _db.SaveChangesAsync(ct); // Persist changes to the database
        }

        public async Task<I18nText> CreateTextAsync(
            string sourceLang,
            string sourceText,
            string context,
            CancellationToken ct)
        {
            var text = new I18nText
            {
                TextKey = Guid.NewGuid().ToString(), // Generate unique key for the text
                SourceLangCode = sourceLang,
                SourceText = sourceText,
                Context = context,
                CreatedAt = DateTime.UtcNow // Set creation timestamp
            };

            _db.I18nTexts.Add(text); // Add new I18nText entity to the context
            await _db.SaveChangesAsync(ct); // Persist changes to the database

            return text; // Return the created entity
        }

        public async Task AddTranslationsAsync(
            long textId,
            Dictionary<string, string> translations,
            string sourceLang,
            CancellationToken ct)
        {
            foreach (var (lang, value) in translations)
            {
                if (lang == sourceLang) continue; // Skip source language

                _db.I18nTranslations.Add(new I18nTranslation
                {
                    TextId = textId,
                    LangCode = lang,
                    TranslatedText = value,
                    UpdatedAt = DateTime.UtcNow // Set update timestamp
                });
            }

            await _db.SaveChangesAsync(ct); // Persist all new translations to the database
        }

        // ============================
        // GET text + translations
        // ============================
        public async Task<I18nText?> GetTextWithTranslationsAsync(
            long textId,
            CancellationToken ct
        )
        {
            return await _db.I18nTexts
                .Include(x => x.I18nTranslations) // Eager load related translations
                .FirstOrDefaultAsync(x => x.TextId == textId, ct); // Find text by ID
        }

        // ============================
        // ADD single translation
        // ============================
        public async Task AddTranslationAsync(
            I18nTranslation translation,
            CancellationToken ct
        )
        {
            await _db.I18nTranslations.AddAsync(translation, ct); // Add translation entity asynchronously
        }
    }

}
