using Core.DTO.Dish;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interface.Service.I18n
{
    public interface IDishI18nService
    {
        Task<DishI18nTextIds> CreateDishI18nTextsAsync(
            Dictionary<string, DishI18nDto> i18n,
            CancellationToken ct
        );
    }

}
