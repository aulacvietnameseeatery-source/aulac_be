using Core.DTO.General;
using Core.DTO.Supplier;
using Core.Entity;
using Core.Interface.Repo;
using Infa.Data;
using Microsoft.EntityFrameworkCore;

namespace Infa.Repo;

/// <summary>
/// Repository implementation for supplier data access operations.
/// </summary>
public class SupplierRepository : ISupplierRepository
{
    private readonly RestaurantMgmtContext _context;

    public SupplierRepository(RestaurantMgmtContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<PagedResultDTO<SupplierDto>> GetAllSuppliersAsync(SupplierListQueryDTO query, CancellationToken cancellationToken = default)
    {
        var dbQuery = _context.Suppliers.AsQueryable();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var searchLower = query.Search.ToLower();
            dbQuery = dbQuery.Where(s =>
                s.SupplierName.ToLower().Contains(searchLower) ||
                (s.Phone != null && s.Phone.ToLower().Contains(searchLower)) ||
                (s.Email != null && s.Email.ToLower().Contains(searchLower))
            );
        }

        // Get total count
        var totalCount = await dbQuery.CountAsync(cancellationToken);

        // Apply pagination
        var suppliers = await dbQuery
            .OrderBy(s => s.SupplierId)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(s => new SupplierDto
            {
                SupplierId = s.SupplierId,
                SupplierName = s.SupplierName,
                Phone = s.Phone,
                Email = s.Email,
                Address = s.Address,
                TaxCode = s.TaxCode
            })
            .ToListAsync(cancellationToken);

        return new PagedResultDTO<SupplierDto>
        {
            PageData = suppliers,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };
    }

    /// <inheritdoc />
    public async Task<Supplier?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _context.Suppliers
            .FirstOrDefaultAsync(s => s.SupplierId == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Supplier?> GetByIdWithIngredientsAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _context.Suppliers
            .Include(s => s.IngredientSuppliers)
                .ThenInclude(is_ => is_.Ingredient)
            .FirstOrDefaultAsync(s => s.SupplierId == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Supplier> CreateAsync(Supplier supplier, CancellationToken cancellationToken = default)
    {
        _context.Suppliers.Add(supplier);
        await _context.SaveChangesAsync(cancellationToken);
        return supplier;
    }

    /// <inheritdoc />
    public async Task<Supplier> UpdateAsync(Supplier supplier, CancellationToken cancellationToken = default)
    {
        _context.Suppliers.Update(supplier);
        await _context.SaveChangesAsync(cancellationToken);
        return supplier;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var supplier = await GetByIdAsync(id, cancellationToken);
        if (supplier == null)
        {
            return false;
        }

        _context.Suppliers.Remove(supplier);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> ExistsByNameAsync(string name, long? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Suppliers.Where(s => s.SupplierName.ToLower() == name.ToLower());

        if (excludeId.HasValue)
        {
            query = query.Where(s => s.SupplierId != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> HasDependenciesAsync(long id, CancellationToken cancellationToken = default)
    {
        var hasIngredients = await _context.IngredientSuppliers
            .AnyAsync(is_ => is_.SupplierId == id, cancellationToken);

        if (hasIngredients)
        {
            return true;
        }

        var hasTransactions = await _context.InventoryTransactions
            .AnyAsync(it => it.SupplierId == id, cancellationToken);

        return hasTransactions;
    }

    /// <inheritdoc />
    public async Task UpdateSupplierIngredientsAsync(long supplierId, List<long> ingredientIds, CancellationToken cancellationToken = default)
    {
        // Remove existing relationships using ExecuteDeleteAsync to avoid tracking issues
        await _context.IngredientSuppliers
            .Where(is_ => is_.SupplierId == supplierId)
            .ExecuteDeleteAsync(cancellationToken);

        // Clear the change tracker to avoid any tracking conflicts
        _context.ChangeTracker.Clear();

        // Add new relationships
        if (ingredientIds.Any())
        {
            var newRelationships = ingredientIds.Select(ingredientId => new IngredientSupplier
            {
                SupplierId = supplierId,
                IngredientId = ingredientId,
                CreatedAt = DateTime.UtcNow
            }).ToList();

            await _context.IngredientSuppliers.AddRangeAsync(newRelationships, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
