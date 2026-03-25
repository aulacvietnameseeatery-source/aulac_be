using Core.DTO.Tax;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Interface.Service.Entity;

public interface ITaxService
{
    Task<List<TaxDTO>> GetAllTaxesAsync(bool onlyActive = true, CancellationToken ct = default);
    Task<TaxDTO?> GetTaxByIdAsync(long taxId, CancellationToken ct = default);
    Task<TaxDTO?> GetDefaultTaxAsync(CancellationToken ct = default);
    Task<long> CreateTaxAsync(CreateTaxRequestDTO request, CancellationToken ct = default);
    Task UpdateTaxAsync(long taxId, UpdateTaxRequestDTO request, CancellationToken ct = default);
    Task DeleteTaxAsync(long taxId, CancellationToken ct = default);
}
