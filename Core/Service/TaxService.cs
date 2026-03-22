using Core.DTO.Tax;
using Core.Entity;
using Core.Exceptions;
using Core.Interface.Repo;
using Core.Interface.Service.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Service;

public class TaxService : ITaxService
{
    private readonly ITaxRepository _taxRepository;
    private readonly IUnitOfWork _uow;

    public TaxService(ITaxRepository taxRepository, IUnitOfWork uow)
    {
        _taxRepository = taxRepository;
        _uow = uow;
    }

    public async Task<List<TaxDTO>> GetAllTaxesAsync(bool onlyActive = true, CancellationToken ct = default)
    {
        var taxes = await _taxRepository.GetAllAsync(onlyActive, ct);
        return taxes.Select(t => MapToDTO(t)).ToList();
    }

    public async Task<TaxDTO?> GetTaxByIdAsync(long taxId, CancellationToken ct = default)
    {
        var tax = await _taxRepository.GetByIdAsync(taxId, ct);
        return tax != null ? MapToDTO(tax) : null;
    }

    public async Task<TaxDTO?> GetDefaultTaxAsync(CancellationToken ct = default)
    {
        var tax = await _taxRepository.GetDefaultTaxAsync(ct);
        return tax != null ? MapToDTO(tax) : null;
    }

    public async Task<long> CreateTaxAsync(CreateTaxRequestDTO request, CancellationToken ct = default)
    {
        var tax = new Tax
        {
            TaxName = request.TaxName,
            TaxRate = request.TaxRate,
            TaxType = request.TaxType,
            IsActive = request.IsActive,
            IsDefault = request.IsDefault,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _taxRepository.AddAsync(tax, ct);
        return tax.TaxId;
    }

    public async Task UpdateTaxAsync(long taxId, UpdateTaxRequestDTO request, CancellationToken ct = default)
    {
        var tax = await _taxRepository.GetByIdAsync(taxId, ct)
            ?? throw new NotFoundException($"Tax with id {taxId} not found.");

        if (request.TaxName != null) tax.TaxName = request.TaxName;
        if (request.TaxRate.HasValue) tax.TaxRate = request.TaxRate.Value;
        if (request.TaxType != null) tax.TaxType = request.TaxType;
        if (request.IsActive.HasValue) tax.IsActive = request.IsActive.Value;
        if (request.IsDefault.HasValue) tax.IsDefault = request.IsDefault.Value;

        tax.UpdatedAt = DateTime.UtcNow;

        await _taxRepository.UpdateAsync(tax, ct);
    }

    public async Task DeleteTaxAsync(long taxId, CancellationToken ct = default)
    {
        await _taxRepository.DeleteAsync(taxId, ct);
    }

    private TaxDTO MapToDTO(Tax tax)
    {
        return new TaxDTO
        {
            TaxId = tax.TaxId,
            TaxName = tax.TaxName,
            TaxRate = tax.TaxRate,
            TaxType = tax.TaxType,
            IsActive = tax.IsActive,
            IsDefault = tax.IsDefault,
            CreatedAt = tax.CreatedAt,
            UpdatedAt = tax.UpdatedAt
        };
    }
}
