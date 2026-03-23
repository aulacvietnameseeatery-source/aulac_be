using Core.Entity;
using Core.Interface.Repo;
using Infa.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Infa.Repo;

public class TaxRepository : ITaxRepository
{
    private readonly RestaurantMgmtContext _context;

    public TaxRepository(RestaurantMgmtContext context)
    {
        _context = context;
    }

    public async Task<List<Tax>> GetAllAsync(bool onlyActive = true, CancellationToken ct = default)
    {
        var query = _context.Taxes.AsQueryable();
        if (onlyActive)
        {
            query = query.Where(t => t.IsActive);
        }
        return await query.ToListAsync(ct);
    }

    public async Task<Tax?> GetByIdAsync(long taxId, CancellationToken ct = default)
    {
        return await _context.Taxes.FirstOrDefaultAsync(t => t.TaxId == taxId, ct);
    }

    public async Task<Tax?> GetDefaultTaxAsync(CancellationToken ct = default)
    {
        return await _context.Taxes.FirstOrDefaultAsync(t => t.IsDefault && t.IsActive, ct);
    }

    public async Task AddAsync(Tax tax, CancellationToken ct = default)
    {
        if (tax.IsDefault)
        {
            await UnsetDefaultTaxesAsync(ct);
        }
        await _context.Taxes.AddAsync(tax, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Tax tax, CancellationToken ct = default)
    {
        if (tax.IsDefault)
        {
            await UnsetDefaultTaxesAsync(ct, tax.TaxId);
        }
        _context.Taxes.Update(tax);
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(long taxId, CancellationToken ct = default)
    {
        var tax = await GetByIdAsync(taxId, ct);
        if (tax != null)
        {
            _context.Taxes.Remove(tax);
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task<bool> ExistsAsync(long taxId, CancellationToken ct = default)
    {
        return await _context.Taxes.AnyAsync(t => t.TaxId == taxId, ct);
    }

    private async Task UnsetDefaultTaxesAsync(CancellationToken ct, long? excludeTaxId = null)
    {
        var defaultTaxes = await _context.Taxes
            .Where(t => t.IsDefault && (!excludeTaxId.HasValue || t.TaxId != excludeTaxId.Value))
            .ToListAsync(ct);

        foreach (var t in defaultTaxes)
        {
            t.IsDefault = false;
        }
    }
}
