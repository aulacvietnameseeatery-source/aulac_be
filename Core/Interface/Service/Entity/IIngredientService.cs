using Core.DTO.Ingredient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interface.Service.Entity
{
    public interface IIngredientService
    {
        Task<(List<IngredientDTO> Items, int TotalCount)> GetListAsync(IngredientFilterParams filter);
        Task<IngredientDTO> GetDetailAsync(long id);
        Task<IngredientDTO> CreateAsync(SaveIngredientRequest request);
        Task<IngredientDTO> UpdateAsync(long id, SaveIngredientRequest request);
        Task DeleteAsync(long id);

        // Stock
        Task AdjustStockAsync(long id, AdjustStockRequest request);
        Task<List<StockHistoryDto>> GetStockHistoryAsync(long id);
    }
}
