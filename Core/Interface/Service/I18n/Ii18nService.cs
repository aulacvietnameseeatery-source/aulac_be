using Core.DTO.Dish;
using Core.DTO.LookUpValue;
using Core.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interface.Service.I18n
{
    public interface Ii18nService
    {
        Task<long> CreateAsync(
            string textKey,
            string context,
            string sourceLang,
            Dictionary<string, string> translations,
            CancellationToken ct
        );

        Task UpdateTextAsync(
            long textId,
            Dictionary<string, DishI18nDto> i18n,
            Func<DishI18nDto, string?> selector,
            CancellationToken ct
        );

        /// <summary>
        /// Updates plain string translations for an existing I18nText entry.
        /// Adds a new translation row when a language doesn't exist yet.
        /// </summary>
        Task UpdateStringsAsync(
            long textId,
            Dictionary<string, string> translations,
            CancellationToken ct
        );

        LookupValueTranslationDto Map(I18nText? text, string? fallback = null);

        Dictionary<long, LookupValueTranslationDto> MapBatch(
            IEnumerable<I18nText> texts);
    }
}
