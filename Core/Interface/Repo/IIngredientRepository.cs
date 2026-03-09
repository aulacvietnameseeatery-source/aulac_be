using Core.DTO.Ingredient;
using Core.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interface.Repo
{
    public interface IIngredientRepository
    {
        Task<(List<Ingredient> Items, int TotalCount)> GetPagedAsync(IngredientFilterParams filter);
        Task<Ingredient?> GetByIdAsync(long id);
        Task<bool> IsNameExistAsync(string name, long? excludeId = null);
        Task<Ingredient> AddAsync(Ingredient ingredient);
        Task UpdateAsync(Ingredient ingredient);
        Task DeleteAsync(Ingredient ingredient);

        // Stock
        Task<List<InventoryTransactionItem>> GetStockHistoryAsync(long ingredientId);
        Task AdjustStockAsync(long ingredientId, decimal quantityChanged, string note);
    }
}
