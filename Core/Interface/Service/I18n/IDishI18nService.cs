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
        /// <summary>
        /// Creates new i18n text entries for a dish in multiple languages.
        /// </summary>
        /// <param name="i18n">Dictionary mapping language codes to dish i18n data.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Text IDs for the created i18n entries.</returns>
        Task<DishI18nTextIds> CreateDishI18nTextsAsync(
            Dictionary<string, DishI18nDto> i18n,
            CancellationToken ct
        );

        /// <summary>
        /// Updates existing i18n text entries for a dish.
        /// </summary>
        /// <param name="textIds">Current text IDs for the dish i18n fields.</param>
        /// <param name="i18n">Dictionary mapping language codes to updated dish i18n data.</param>
        /// <param name="ct">Cancellation token.</param>
        Task UpdateDishI18nTextsAsync(
            DishI18nTextIds textIds,
            Dictionary<string, DishI18nDto> i18n,
            CancellationToken ct
        );

        /// <summary>
        /// Creates new or updates existing i18n text entries for a dish.
        /// </summary>
        /// <param name="current">Current text IDs for the dish i18n fields.</param>
        /// <param name="i18n">Dictionary mapping language codes to dish i18n data.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Text IDs for the created or updated i18n entries.</returns>
        Task<DishI18nTextIds> CreateOrUpdateDishI18nTextsAsync(
            DishI18nTextIds current,
            Dictionary<string, DishI18nDto> i18n,
            CancellationToken ct
        );
    }

}
