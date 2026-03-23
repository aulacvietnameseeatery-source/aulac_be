using Core.DTO.General;
using Core.DTO.Inventory;
using Core.Entity;
using Core.Interface.Repo;
using Infa.Data;
using Microsoft.EntityFrameworkCore;

namespace Infa.Repo;

public class InventoryRepository : IInventoryRepository
{
    private readonly RestaurantMgmtContext _context;

    public InventoryRepository(RestaurantMgmtContext context)
    {
        _context = context;
    }

    // ──────────────────────────────────────────────────────────
    // Inventory Items
    // ──────────────────────────────────────────────────────────

    public async Task<PagedResultDTO<InventoryItemDto>> GetItemsPagedAsync(
        GetInventoryItemsFilterRequest filter, CancellationToken ct = default)
    {
        var query = _context.Ingredients
            .Include(i => i.TypeLv)
            .Include(i => i.UnitLv)
            .Include(i => i.CategoryLv)
            .Include(i => i.Image)
            .Include(i => i.CurrentStock)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
            query = query.Where(i => i.IngredientName.Contains(filter.Search));

        if (filter.CategoryLvId.HasValue)
            query = query.Where(i => i.CategoryLvId == filter.CategoryLvId);

        if (filter.TypeLvId.HasValue)
            query = query.Where(i => i.TypeLvId == filter.TypeLvId);

        if (filter.IsLowStock == true)
        {
            query = query.Where(i => i.CurrentStock != null &&
                                     i.CurrentStock.MinStockLevel > 0 &&
                                     i.CurrentStock.QuantityOnHand <= i.CurrentStock.MinStockLevel);
        }

        query = query.OrderByDescending(i => i.IngredientId);

        int totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((filter.PageIndex - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(i => new InventoryItemDto
            {
                IngredientId = i.IngredientId,
                IngredientName = i.IngredientName,
                UnitLvId = i.UnitLvId,
                UnitName = i.UnitLv != null ? i.UnitLv.ValueName : null,
                TypeLvId = i.TypeLvId,
                TypeName = i.TypeLv != null ? i.TypeLv.ValueName : null,
                CategoryLvId = i.CategoryLvId,
                CategoryCode = i.CategoryLv != null ? i.CategoryLv.ValueCode : null,
                CategoryName = i.CategoryLv != null ? i.CategoryLv.ValueName : null,
                ImageId = i.ImageId,
                ImageUrl = i.Image != null ? i.Image.Url : null,
                QuantityOnHand = i.CurrentStock != null ? i.CurrentStock.QuantityOnHand : 0,
                MinStockLevel = i.CurrentStock != null ? i.CurrentStock.MinStockLevel : 0,
                LastUpdatedAt = i.CurrentStock != null ? i.CurrentStock.LastUpdatedAt : null,
            })
            .ToListAsync(ct);

        return new PagedResultDTO<InventoryItemDto>
        {
            PageData = items,
            PageIndex = filter.PageIndex,
            PageSize = filter.PageSize,
            TotalCount = totalCount,
        };
    }

    // ──────────────────────────────────────────────────────────
    // Transactions — Create
    // ──────────────────────────────────────────────────────────

    public async Task<long> CreateTransactionAsync(
        InventoryTransaction transaction, List<InventoryTransactionItem> items, CancellationToken ct = default)
    {
        await _context.InventoryTransactions.AddAsync(transaction, ct);
        await _context.SaveChangesAsync(ct);

        foreach (var item in items)
        {
            item.TransactionId = transaction.TransactionId;
        }
        await _context.InventoryTransactionItems.AddRangeAsync(items, ct);
        await _context.SaveChangesAsync(ct);

        return transaction.TransactionId;
    }

    // ──────────────────────────────────────────────────────────
    // Transactions — Read
    // ──────────────────────────────────────────────────────────

    public async Task<InventoryTransaction?> GetTransactionByIdAsync(long transactionId, CancellationToken ct = default)
    {
        return await _context.InventoryTransactions
            .Include(t => t.TypeLv)
            .Include(t => t.StatusLv)
            .Include(t => t.ExportReasonLv)
            .Include(t => t.Supplier)
            .Include(t => t.CreatedByNavigation)
            .Include(t => t.ApprovedByNavigation)
            .Include(t => t.InventoryTransactionItems)
                .ThenInclude(ti => ti.Ingredient)
                    .ThenInclude(i => i.Image)
            .Include(t => t.InventoryTransactionItems)
                .ThenInclude(ti => ti.Ingredient)
                    .ThenInclude(i => i.CategoryLv)
            .Include(t => t.InventoryTransactionItems)
                .ThenInclude(ti => ti.UnitLv)
            .Include(t => t.InventoryTransactionItems)
                .ThenInclude(ti => ti.VarianceReasonLv)
            .Include(t => t.InventoryTransactionMedia)
                .ThenInclude(m => m.Media)
            .FirstOrDefaultAsync(t => t.TransactionId == transactionId, ct);
    }

    public async Task<PagedResultDTO<InventoryTransactionListDto>> GetTransactionsPagedAsync(
        GetTransactionsFilterRequest filter, CancellationToken ct = default)
    {
        var query = _context.InventoryTransactions
            .Include(t => t.TypeLv)
            .Include(t => t.StatusLv)
            .Include(t => t.ExportReasonLv)
            .Include(t => t.Supplier)
            .Include(t => t.CreatedByNavigation)
            .Include(t => t.ApprovedByNavigation)
            .Include(t => t.InventoryTransactionItems)
            .AsQueryable();

        // Filters
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            query = query.Where(t =>
                (t.TransactionCode != null && t.TransactionCode.Contains(filter.Search)) ||
                (t.Note != null && t.Note.Contains(filter.Search)));
        }

        if (filter.TypeLvId.HasValue)
            query = query.Where(t => t.TypeLvId == filter.TypeLvId);

        if (filter.StatusLvId.HasValue)
            query = query.Where(t => t.StatusLvId == filter.StatusLvId);

        if (filter.ExportReasonLvId.HasValue)
            query = query.Where(t => t.ExportReasonLvId == filter.ExportReasonLvId);

        if (filter.SupplierId.HasValue)
            query = query.Where(t => t.SupplierId == filter.SupplierId);

        if (filter.FromDate.HasValue)
            query = query.Where(t => t.CreatedAt >= filter.FromDate.Value.Date);

        if (filter.ToDate.HasValue)
            query = query.Where(t => t.CreatedAt < filter.ToDate.Value.Date.AddDays(1));

        query = query.OrderByDescending(t => t.CreatedAt);

        int totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((filter.PageIndex - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(t => new InventoryTransactionListDto
            {
                TransactionId = t.TransactionId,
                TransactionCode = t.TransactionCode,
                TypeLvId = t.TypeLvId,
                TypeCode = t.TypeLv.ValueCode,
                TypeName = t.TypeLv.ValueName,
                StatusLvId = t.StatusLvId,
                StatusCode = t.StatusLv.ValueCode,
                StatusName = t.StatusLv.ValueName,
                ExportReasonLvId = t.ExportReasonLvId,
                ExportReasonName = t.ExportReasonLv != null ? t.ExportReasonLv.ValueName : null,
                SupplierId = t.SupplierId,
                SupplierName = t.Supplier != null ? t.Supplier.SupplierName : null,
                CreatedBy = t.CreatedBy,
                CreatedByName = t.CreatedByNavigation != null ? t.CreatedByNavigation.FullName : null,
                CreatedAt = t.CreatedAt,
                SubmittedAt = t.SubmittedAt,
                ApprovedBy = t.ApprovedBy,
                ApprovedByName = t.ApprovedByNavigation != null ? t.ApprovedByNavigation.FullName : null,
                ApprovedAt = t.ApprovedAt,
                Note = t.Note,
                ItemCount = t.InventoryTransactionItems.Count,
                TotalValue = t.InventoryTransactionItems
                    .Where(ti => ti.UnitPrice.HasValue)
                    .Sum(ti => ti.Quantity * (ti.UnitPrice ?? 0)),
            })
            .ToListAsync(ct);

        return new PagedResultDTO<InventoryTransactionListDto>
        {
            PageData = items,
            PageIndex = filter.PageIndex,
            PageSize = filter.PageSize,
            TotalCount = totalCount,
        };
    }

    // ──────────────────────────────────────────────────────────
    // Transactions — Update
    // ──────────────────────────────────────────────────────────

    public async Task UpdateTransactionAsync(InventoryTransaction transaction, CancellationToken ct = default)
    {
        _context.InventoryTransactions.Update(transaction);
        await _context.SaveChangesAsync(ct);
    }

    // ──────────────────────────────────────────────────────────
    // Stock
    // ──────────────────────────────────────────────────────────

    public async Task<CurrentStock?> GetCurrentStockAsync(long ingredientId, CancellationToken ct = default)
    {
        return await _context.CurrentStocks
            .FirstOrDefaultAsync(s => s.IngredientId == ingredientId, ct);
    }

    public async Task UpdateStockAsync(long ingredientId, decimal quantityChange, CancellationToken ct = default)
    {
        var stock = await _context.CurrentStocks
            .FirstOrDefaultAsync(s => s.IngredientId == ingredientId, ct);

        if (stock == null)
        {
            // Tạo record CurrentStock mới nếu chưa tồn tại
            stock = new CurrentStock
            {
                IngredientId = ingredientId,
                QuantityOnHand = quantityChange,
                MinStockLevel = 0,
                LastUpdatedAt = DateTime.UtcNow,
            };
            await _context.CurrentStocks.AddAsync(stock, ct);
        }
        else
        {
            stock.QuantityOnHand += quantityChange;
            stock.LastUpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(ct);
    }

    public async Task<PagedResultDTO<StockCardDto>> GetStockCardAsync(
        long ingredientId, int pageIndex, int pageSize, CancellationToken ct = default)
    {
        var query = _context.InventoryTransactionItems
            .Include(ti => ti.Transaction)
                .ThenInclude(t => t.TypeLv)
            .Include(ti => ti.Transaction)
                .ThenInclude(t => t.StatusLv)
            .Include(ti => ti.Transaction)
                .ThenInclude(t => t.ExportReasonLv)
            .Include(ti => ti.Transaction)
                .ThenInclude(t => t.CreatedByNavigation)
            .Include(ti => ti.UnitLv)
            .Where(ti => ti.IngredientId == ingredientId)
            .OrderByDescending(ti => ti.Transaction.CreatedAt);

        int totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(ti => new StockCardDto
            {
                TransactionItemId = ti.TransactionItemId,
                TransactionId = ti.TransactionId,
                TransactionCode = ti.Transaction.TransactionCode,
                TypeCode = ti.Transaction.TypeLv.ValueCode,
                TypeName = ti.Transaction.TypeLv.ValueName,
                StatusCode = ti.Transaction.StatusLv.ValueCode,
                QuantityChanged = ti.Quantity,
                UnitName = ti.UnitLv != null ? ti.UnitLv.ValueName : null,
                UnitPrice = ti.UnitPrice,
                ExportReasonName = ti.Transaction.ExportReasonLv != null ? ti.Transaction.ExportReasonLv.ValueName : null,
                Note = ti.Note,
                CreatedByName = ti.Transaction.CreatedByNavigation != null ? ti.Transaction.CreatedByNavigation.FullName : null,
                CreatedAt = ti.Transaction.CreatedAt,
            })
            .ToListAsync(ct);

        return new PagedResultDTO<StockCardDto>
        {
            PageData = items,
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }

    // ──────────────────────────────────────────────────────────
    // Dashboard
    // ──────────────────────────────────────────────────────────

    public async Task<InventoryDashboardDto> GetDashboardAsync(CancellationToken ct = default)
    {
        var totalItems = await _context.Ingredients.CountAsync(ct);

        var lowStockQuery = _context.Ingredients
            .Include(i => i.CurrentStock)
            .Include(i => i.CategoryLv)
            .Include(i => i.UnitLv)
            .Where(i => i.CurrentStock != null &&
                        i.CurrentStock.MinStockLevel > 0 &&
                        i.CurrentStock.QuantityOnHand <= i.CurrentStock.MinStockLevel);

        var lowStockItems = await lowStockQuery.CountAsync(ct);

        var outOfStockItems = await _context.Ingredients
            .Include(i => i.CurrentStock)
            .Where(i => i.CurrentStock != null && i.CurrentStock.QuantityOnHand <= 0)
            .CountAsync(ct);

        var pendingTransactions = await _context.InventoryTransactions
            .Include(t => t.StatusLv)
            .Where(t => t.StatusLv.ValueCode == "PENDING_APPROVAL")
            .CountAsync(ct);

        var lowStockList = await lowStockQuery
            .OrderBy(i => i.CurrentStock!.QuantityOnHand)
            .Take(10)
            .Select(i => new LowStockItemDto
            {
                IngredientId = i.IngredientId,
                IngredientName = i.IngredientName,
                CategoryName = i.CategoryLv != null ? i.CategoryLv.ValueName : null,
                QuantityOnHand = i.CurrentStock!.QuantityOnHand,
                MinStockLevel = i.CurrentStock.MinStockLevel,
                UnitName = i.UnitLv != null ? i.UnitLv.ValueName : null,
            })
            .ToListAsync(ct);

        var recentTransactions = await _context.InventoryTransactions
            .Include(t => t.TypeLv)
            .Include(t => t.StatusLv)
            .Include(t => t.CreatedByNavigation)
            .Include(t => t.InventoryTransactionItems)
            .OrderByDescending(t => t.CreatedAt)
            .Take(10)
            .Select(t => new RecentTransactionDto
            {
                TransactionId = t.TransactionId,
                TransactionCode = t.TransactionCode,
                TypeName = t.TypeLv.ValueName,
                StatusName = t.StatusLv.ValueName,
                CreatedByName = t.CreatedByNavigation != null ? t.CreatedByNavigation.FullName : null,
                CreatedAt = t.CreatedAt,
                ItemCount = t.InventoryTransactionItems.Count,
            })
            .ToListAsync(ct);

        return new InventoryDashboardDto
        {
            TotalItems = totalItems,
            LowStockItems = lowStockItems,
            OutOfStockItems = outOfStockItems,
            PendingTransactions = pendingTransactions,
            LowStockList = lowStockList,
            RecentTransactions = recentTransactions,
        };
    }

    // ──────────────────────────────────────────────────────────
    // Transaction Code Counter
    // ──────────────────────────────────────────────────────────

    public async Task<int> GetTodayTransactionCountByTypeAsync(string typeCode, CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;
        return await _context.InventoryTransactions
            .Include(t => t.TypeLv)
            .Where(t => t.TypeLv.ValueCode == typeCode && t.CreatedAt >= today)
            .CountAsync(ct);
    }
}
