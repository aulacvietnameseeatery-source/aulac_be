using Core.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interface.Repo
{
    public interface II18nRepository
    {
        /// <summary>
        /// Creates a new I18nText entry with the specified source language, text, and context.
        /// </summary>
        /// <param name="sourceLang">The language code of the source text.</param>
        /// <param name="sourceText">The original text to be translated.</param>
        /// <param name="context">The context in which the text is used.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The created I18nText entity.</returns>
        Task<I18nText> CreateTextAsync(
            string sourceLang,
            string sourceText,
            string context,
            CancellationToken ct);

        /// <summary>
        /// Adds multiple translations for a given text ID.
        /// </summary>
        /// <param name="textId">The ID of the I18nText to add translations for.</param>
        /// <param name="translations">A dictionary of language codes and their corresponding translations.</param>
        /// <param name="sourceLang">The source language code.</param>
        /// <param name="ct">Cancellation token.</param>
        Task AddTranslationsAsync(
            long textId,
            Dictionary<string, string> translations,
            string sourceLang,
            CancellationToken ct);

        /// <summary>
        /// Adds a new I18nText entity to the repository.
        /// </summary>
        /// <param name="text">The I18nText entity to add.</param>
        /// <param name="ct">Cancellation token.</param>
        Task AddAsync(I18nText text, CancellationToken ct);

        /// <summary>
        /// Retrieves an I18nText entity along with its translations by text ID.
        /// </summary>
        /// <param name="textId">The ID of the I18nText to retrieve.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The I18nText entity with its translations, or null if not found.</returns>
        Task<I18nText?> GetTextWithTranslationsAsync(
            long textId,
            CancellationToken ct);

        /// <summary>
        /// Adds a single translation to the repository.
        /// </summary>
        /// <param name="translation">The I18nTranslation entity to add.</param>
        /// <param name="ct">Cancellation token.</param>
        Task AddTranslationAsync(
            I18nTranslation translation,
            CancellationToken ct);
    }
}
