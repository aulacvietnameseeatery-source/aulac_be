using Core.DTO.Dish;
using Core.Interface.Service.I18n;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Service
{
    public class DishI18nService : IDishI18nService
    {
        private readonly Ii18nService _i18nTextService;

        public DishI18nService(Ii18nService i18nTextService)
        {
            _i18nTextService = i18nTextService;
        }

        public async Task<DishI18nTextIds> CreateDishI18nTextsAsync(
            Dictionary<string, DishI18nDto> i18n,
            CancellationToken ct)
        {
            if (!i18n.Any())
                throw new InvalidOperationException("I18n is empty");

            var sourceLang = i18n.Keys.First(); // ví dụ: en

            var result = new DishI18nTextIds();

            result.DishNameTextId = await CreateText(
                "dish.name",
                "Dish Name",
                sourceLang,
                i18n.ToDictionary(x => x.Key, x => x.Value.DishName),
                ct
            );

            result.DescriptionTextId = await CreateOptionalText(
                "dish.description",
                "Dish Description",
                sourceLang,
                i18n,
                x => x.Description,
                ct
            );

            result.SloganTextId = await CreateOptionalText(
                "dish.slogan",
                "Dish Slogan",
                sourceLang,
                i18n,
                x => x.Slogan,
                ct
            );

            result.NoteTextId = await CreateOptionalText(
                "dish.note",
                "Dish Note",
                sourceLang,
                i18n,
                x => x.Note,
                ct
            );

            result.ShortDescriptionTextId = await CreateOptionalText(
                "dish.short_description",
                "Dish Short Description",
                sourceLang,
                i18n,
                x => x.ShortDescription,
                ct
            );

            return result;
        }

        // ---------- helpers ----------

        private async Task<long> CreateText(
            string keyPrefix,
            string context,
            string sourceLang,
            Dictionary<string, string> translations,
            CancellationToken ct)
        {
            return await _i18nTextService.CreateAsync(
                $"{keyPrefix}.{Guid.NewGuid()}",
                context,
                sourceLang,
                translations,
                ct
            );
        }

        private async Task<long?> CreateOptionalText(
            string keyPrefix,
            string context,
            string sourceLang,
            Dictionary<string, DishI18nDto> i18n,
            Func<DishI18nDto, string?> selector,
            CancellationToken ct)
        {
            var values = i18n
                .Where(x => !string.IsNullOrWhiteSpace(selector(x.Value)))
                .ToDictionary(x => x.Key, x => selector(x.Value)!);

            if (!values.Any())
                return null;

            return await CreateText(keyPrefix, context, sourceLang, values, ct);
        }
    }

}
