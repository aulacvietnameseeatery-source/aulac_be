using Core.DTO.Dish;
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
    }
}
