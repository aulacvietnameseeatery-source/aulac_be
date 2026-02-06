using Core.DTO.Dish;
using Core.Entity;
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
                throw new InvalidOperationException("I18n is empty"); // Ensure i18n dictionary is not empty

            var sourceLang = i18n.Keys.First(); // Get the first language key as the source language

            var result = new DishI18nTextIds();

            result.DishNameTextId = await CreateText(
                "dish.name",
                "Dish Name",
                sourceLang,
                i18n.ToDictionary(x => x.Key, x => x.Value.DishName), // Map language to dish name
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
                $"{keyPrefix}.{Guid.NewGuid()}", // Generate unique key for the text
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
                .Where(x => !string.IsNullOrWhiteSpace(selector(x.Value))) // Filter out empty or null values
                .ToDictionary(x => x.Key, x => selector(x.Value)!);

            if (!values.Any())
                return null; // Return null if no values to create

            return await CreateText(keyPrefix, context, sourceLang, values, ct);
        }

        public async Task UpdateDishI18nTextsAsync(DishI18nTextIds textIds, Dictionary<string, DishI18nDto> i18n, CancellationToken ct)
        {
            await _i18nTextService.UpdateTextAsync(textIds.DishNameTextId, i18n, x => x.DishName, ct); // Update required field

            if (textIds.DescriptionTextId.HasValue)
                await _i18nTextService.UpdateTextAsync(textIds.DescriptionTextId.Value, i18n, x => x.Description, ct);

            if (textIds.ShortDescriptionTextId.HasValue)
                await _i18nTextService.UpdateTextAsync(textIds.ShortDescriptionTextId.Value, i18n, x => x.ShortDescription, ct);

            if (textIds.SloganTextId.HasValue)
                await _i18nTextService.UpdateTextAsync(textIds.SloganTextId.Value, i18n, x => x.Slogan, ct);

            if (textIds.NoteTextId.HasValue)
                await _i18nTextService.UpdateTextAsync(textIds.NoteTextId.Value, i18n, x => x.Note, ct);
        }

        public async Task<DishI18nTextIds> CreateOrUpdateDishI18nTextsAsync(DishI18nTextIds current, Dictionary<string, DishI18nDto> i18n, CancellationToken ct)
        {
            var result = new DishI18nTextIds
            {
                DishNameTextId = current.DishNameTextId,
                DescriptionTextId = current.DescriptionTextId,
                ShortDescriptionTextId = current.ShortDescriptionTextId,
                SloganTextId = current.SloganTextId,
                NoteTextId = current.NoteTextId
            };

            // Dish Name (required)
            await _i18nTextService.UpdateTextAsync(
                result.DishNameTextId,
                i18n,
                x => x.DishName,
                ct
            );

            // Optional fields
            result.DescriptionTextId =
                await CreateOrUpdateOptional(
                    result.DescriptionTextId,
                    "dish.description",
                    "Dish Description",
                    i18n,
                    x => x.Description,
                    ct
                );

            result.ShortDescriptionTextId =
                await CreateOrUpdateOptional(
                    result.ShortDescriptionTextId,
                    "dish.short_description",
                    "Dish Short Description",
                    i18n,
                    x => x.ShortDescription,
                    ct
                );

            result.SloganTextId =
                await CreateOrUpdateOptional(
                    result.SloganTextId,
                    "dish.slogan",
                    "Dish Slogan",
                    i18n,
                    x => x.Slogan,
                    ct
                );

            result.NoteTextId =
                await CreateOrUpdateOptional(
                    result.NoteTextId,
                    "dish.note",
                    "Dish Note",
                    i18n,
                    x => x.Note,
                    ct
                );

            return result;
        }

        private async Task<long?> CreateOrUpdateOptional(
            long? currentTextId,
            string keyPrefix,
            string context,
            Dictionary<string, DishI18nDto> i18n,
            Func<DishI18nDto, string?> selector,
            CancellationToken ct)
        {
            var values = i18n
                .Where(x => !string.IsNullOrWhiteSpace(selector(x.Value))) // Filter out empty or null values
                .ToDictionary(x => x.Key, x => selector(x.Value)!);

            if (!values.Any())
                return currentTextId; // If no values, return current text id

            // Update
            if (currentTextId.HasValue)
            {
                await _i18nTextService.UpdateTextAsync(
                    currentTextId.Value,
                    i18n,
                    selector,
                    ct
                );
                return currentTextId;
            }

            // Create
            var sourceLang = values.Keys.First(); // Use first language as source

            return await _i18nTextService.CreateAsync(
                $"{keyPrefix}.{Guid.NewGuid()}",
                context,
                sourceLang,
                values,
                ct
            );
        }

    }

}
