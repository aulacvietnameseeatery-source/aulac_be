using Core.Entity;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Interface.Repo;

public interface ITaxRepository
{
    Task<List<Tax>> GetAllAsync(bool onlyActive = true, CancellationToken ct = default);
    Task<Tax?> GetByIdAsync(long taxId, CancellationToken ct = default);
    Task<Tax?> GetDefaultTaxAsync(CancellationToken ct = default);
    Task AddAsync(Tax tax, CancellationToken ct = default);
    Task UpdateAsync(Tax tax, CancellationToken ct = default);
    Task DeleteAsync(long taxId, CancellationToken ct = default);
    Task<bool> ExistsAsync(long taxId, CancellationToken ct = default);
}
