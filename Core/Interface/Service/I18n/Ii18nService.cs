using Core.DTO.Dish;
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
    }
}
