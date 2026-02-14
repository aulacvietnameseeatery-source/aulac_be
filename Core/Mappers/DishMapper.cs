using Core.DTO.Dish;
using Core.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Core.Mappers
{
    public static class DishMapper
    {
        public static DishDetailForActionsDto ToDetailDto(Dish dish, DishTag dishTag, DishTag? dishDiet)
        {
            return new DishDetailForActionsDto
            {
                DishId = dish.DishId,
                CategoryId = dish.CategoryId,
                CategoryName = dish.Category.CategoryName,
                Price = dish.Price,
                DishStatusLvId = dish.DishStatusLvId,
                DishStatus = dish.DishStatusLv.ValueCode,
                TagId = dishTag.TagId,
                TagName = dishTag.Tag.ValueCode,
                DietId = dishDiet?.TagId,
                DietName = dishDiet?.Tag.ValueCode,
                IsOnline = dish.IsOnline ?? false,
                ChefRecommended = dish.ChefRecommended ?? false,
                DisplayOrder = dish.DisplayOrder,
                Calories = dish.Calories,
                PrepTimeMinutes = dish.PrepTimeMinutes,
                CookTimeMinutes = dish.CookTimeMinutes,

                I18n = MergeI18n(
                    dish.DishNameText,
                    dish.DescriptionText,
                    dish.ShortDescriptionText,
                    dish.SloganText,
                    dish.NoteText
                ),

                Media = dish.DishMedia.Select(m => new DishMediaDto
                {
                    MediaId = m.MediaId,
                    Url = m.Media.Url,
                    MediaType = m.Media.MediaTypeLv.ValueCode,
                    IsPrimary = m.IsPrimary ?? false
                }).ToList()
            };
        }

        private static Dictionary<string, DishI18nDto> MergeI18n(
            I18nText dishName,
            I18nText? description,
            I18nText? shortDesc,
            I18nText? slogan,
            I18nText? note
        )
        {
            var result = new Dictionary<string, DishI18nDto>();

            void Apply(I18nText? text, Action<DishI18nDto, string> setter)
            {
                if (text == null) return;

                result.TryAdd(text.SourceLangCode, new DishI18nDto());
                setter(result[text.SourceLangCode], text.SourceText);

                foreach (var tr in text.I18nTranslations)
                {
                    result.TryAdd(tr.LangCode, new DishI18nDto());
                    setter(result[tr.LangCode], tr.TranslatedText);
                }
            }

            Apply(dishName, (x, v) => x.DishName = v);
            Apply(description, (x, v) => x.Description = v);
            Apply(shortDesc, (x, v) => x.ShortDescription = v);
            Apply(slogan, (x, v) => x.Slogan = v);
            Apply(note, (x, v) => x.Note = v);

            return result;
        }
    }
}
