using Core.DTO.Dish;
using Core.DTO.LookUpValue;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interface.Service.Others
{
    public interface ITranslationService
    {
        Task<Dictionary<string, DishI18nDto>> TranslateDishAsync(
            string sourceLang,
            DishI18nDto sourceData);

        Task<Dictionary<string, Dictionary<string, string>>> TranslateSystemSettingsAsync(
            string sourceLang,
            Dictionary<string, string> sourceData);

        Task<Dictionary<string, LookupDto>> TranslateLookupAsync(
            string sourceLang,
            LookupDto sourceData);
    }
}
