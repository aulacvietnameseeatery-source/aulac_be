using Core.DTO.Ingredient;
using Core.Entity;
using Core.Interface.Repo;
using Infa.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infa.Repo
{
    public class IngredientRepository : IIngredientRepository
    {
        private readonly RestaurantMgmtContext _context;

        public IngredientRepository(RestaurantMgmtContext context)
        {
            _context = context;
        }

        public async Task<(List<Ingredient> Items, int TotalCount)> GetPagedAsync(IngredientFilterParams filter)
        {
            var query = _context.Ingredients
                .Include(i => i.TypeLv)
                .Include(i => i.UnitLv)
                .Include(i => i.Image)
                .Include(i => i.CurrentStock)
                .Include(i => i.IngredientSuppliers)
                    .ThenInclude(ise => ise.Supplier)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Search))
                query = query.Where(i => i.IngredientName.Contains(filter.Search));

            if (filter.TypeLvId.HasValue)
                query = query.Where(i => i.TypeLvId == filter.TypeLvId);

            // Logic lọc món sắp hết hàng
            if (filter.IsLowStock == true)
            {
                query = query.Where(i => i.CurrentStock != null &&
                                         i.CurrentStock.QuantityOnHand <= i.CurrentStock.MinStockLevel);
            }

            query = query.OrderByDescending(i => i.IngredientId);

            // Tính toán
            int totalCount = await query.CountAsync();
            var items = await query.Skip((filter.PageIndex - 1) * filter.PageSize)
                                   .Take(filter.PageSize)
                                   .ToListAsync();

            // Trả về Tuple
            return (items, totalCount);
        }

        public async Task<Ingredient?> GetByIdAsync(long id)
        {
            return await _context.Ingredients
                .Include(i => i.TypeLv)
                .Include(i => i.UnitLv)
                .Include(i => i.Image)
                .Include(i => i.CurrentStock)
                .Include(i => i.IngredientSuppliers)
                    .ThenInclude(ise => ise.Supplier)
                .FirstOrDefaultAsync(i => i.IngredientId == id);
        }

        public async Task<bool> IsNameExistAsync(string name, long? excludeId = null)
        {
            var query = _context.Ingredients.Where(i => i.IngredientName.ToLower() == name.ToLower());
            if (excludeId.HasValue) query = query.Where(i => i.IngredientId != excludeId.Value);
            return await query.AnyAsync();
        }

        public async Task<Ingredient> AddAsync(Ingredient ingredient)
        {
            await _context.Ingredients.AddAsync(ingredient);
            await _context.SaveChangesAsync();
            return ingredient;
        }

        public async Task UpdateAsync(Ingredient ingredient)
        {
            _context.Ingredients.Update(ingredient);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Ingredient ingredient)
        {
            _context.Ingredients.Remove(ingredient);
            await _context.SaveChangesAsync();
        }

        public async Task<List<InventoryTransactionItem>> GetStockHistoryAsync(long ingredientId)
        {
            return await _context.InventoryTransactionItems
                .Include(tx => tx.Transaction) 
                .Where(tx => tx.IngredientId == ingredientId)
                .OrderByDescending(tx => tx.Transaction.CreatedAt) 
                .Take(50)
                .ToListAsync();
        }

        public async Task AdjustStockAsync(long ingredientId, decimal quantityChanged, string note)
        {
            var stock = await _context.CurrentStocks.FirstOrDefaultAsync(s => s.IngredientId == ingredientId);
            if (stock == null) throw new Exception("Stock record not found.");

            stock.QuantityOnHand += quantityChanged;
            stock.LastUpdatedAt = DateTime.UtcNow;

            var ingredient = await _context.Ingredients.FirstOrDefaultAsync(i => i.IngredientId == ingredientId);
            string unit = ingredient?.UnitLvId.ToString() ?? "unit";

            // InventoryTransaction
            var parentTransaction = new InventoryTransaction
            {
                CreatedAt = DateTime.UtcNow,

                // TODO: TransactionType = quantityChanged > 0 ? "IMPORT" : "EXPORT"
                // Status = "COMPLETED", ...
            };

            // InventoryTransactionItem
            // _OLD: Unit = unit (string field removed, now uses UnitLvId FK)
            var txItem = new InventoryTransactionItem
            {
                IngredientId = ingredientId,
                Quantity = quantityChanged,
                UnitLvId = ingredient?.UnitLvId ?? 0,
                Note = note,
                Transaction = parentTransaction
            };

            await _context.InventoryTransactionItems.AddAsync(txItem);
            await _context.SaveChangesAsync();
        }
    }
}
