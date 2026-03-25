using Core.DTO.General;
using Core.DTO.Ingredient;
using Core.DTO.Supplier;
using Core.Entity;
using Core.Interface.Repo;
using Core.Interface.Service.Entity;
using Microsoft.Extensions.Logging;

namespace Core.Service;

/// <summary>
/// Implementation of ISupplierService handling supplier business logic
/// </summary>
public class SupplierService : ISupplierService
{
    private readonly ISupplierRepository _supplierRepository;
    private readonly ILogger<SupplierService> _logger;

    public SupplierService(
        ISupplierRepository supplierRepository,
        ILogger<SupplierService> logger)
    {
        _supplierRepository = supplierRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<PagedResultDTO<SupplierDto>> GetAllSuppliersAsync(SupplierListQueryDTO query, CancellationToken cancellationToken = default)
    {
        return await _supplierRepository.GetAllSuppliersAsync(query, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SupplierDetailDto> GetSupplierDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        var supplier = await _supplierRepository.GetByIdWithIngredientsAsync(id, cancellationToken);
        
        if (supplier == null)
        {
            _logger.LogWarning("Supplier with ID {SupplierId} not found", id);
            throw new KeyNotFoundException($"Supplier with ID {id} not found.");
        }

        return new SupplierDetailDto
        {
            SupplierId = supplier.SupplierId,
            SupplierName = supplier.SupplierName,
            Phone = supplier.Phone,
            Email = supplier.Email,
            Address = supplier.Address,
            TaxCode = supplier.TaxCode,
            Ingredients = supplier.IngredientSuppliers
                .Where(is_ => is_.Ingredient != null)
                .Select(is_ => new IngredientDTO
                {
                    IngredientId = is_.Ingredient!.IngredientId,
                    IngredientName = is_.Ingredient.IngredientName,
                    UnitLvId = is_.Ingredient.UnitLvId
                })
                .ToList()
        };
    }

    /// <inheritdoc />
    public async Task<SupplierDto> CreateSupplierAsync(CreateSupplierRequest request, CancellationToken cancellationToken = default)
    {
        // Check if supplier name already exists
        var nameExists = await _supplierRepository.ExistsByNameAsync(request.SupplierName, null, cancellationToken);
        if (nameExists)
        {
            _logger.LogWarning("Cannot create supplier: name '{SupplierName}' already exists", request.SupplierName);
            throw new InvalidOperationException($"Supplier name '{request.SupplierName}' already exists.");
        }

        var supplier = new Supplier
        {
            SupplierName = request.SupplierName,
            Phone = request.Phone,
            Email = request.Email,
            Address = request.Address,
            TaxCode = request.TaxCode
        };

        var createdSupplier = await _supplierRepository.CreateAsync(supplier, cancellationToken);
        
        // Update ingredient relationships
        if (request.IngredientIds.Any())
        {
            await _supplierRepository.UpdateSupplierIngredientsAsync(createdSupplier.SupplierId, request.IngredientIds, cancellationToken);
        }
        
        _logger.LogInformation("Created new supplier: {SupplierName} (ID: {SupplierId})", 
            createdSupplier.SupplierName, createdSupplier.SupplierId);
        
        return MapToDto(createdSupplier);
    }

    /// <inheritdoc />
    public async Task<SupplierDto> UpdateSupplierAsync(long id, UpdateSupplierRequest request, CancellationToken cancellationToken = default)
    {
        var supplier = await _supplierRepository.GetByIdAsync(id, cancellationToken);
        
        if (supplier == null)
        {
            _logger.LogWarning("Supplier with ID {SupplierId} not found", id);
            throw new KeyNotFoundException($"Supplier with ID {id} not found.");
        }

        // Check if supplier name already exists (exclude current supplier)
        var nameExists = await _supplierRepository.ExistsByNameAsync(request.SupplierName, id, cancellationToken);
        if (nameExists)
        {
            _logger.LogWarning("Cannot update supplier: name '{SupplierName}' already exists", request.SupplierName);
            throw new InvalidOperationException($"Supplier name '{request.SupplierName}' already exists.");
        }

        // Update supplier properties
        supplier.SupplierName = request.SupplierName;
        supplier.Phone = request.Phone;
        supplier.Email = request.Email;
        supplier.Address = request.Address;
        supplier.TaxCode = request.TaxCode;

        var updatedSupplier = await _supplierRepository.UpdateAsync(supplier, cancellationToken);
        
        // Update ingredient relationships
        await _supplierRepository.UpdateSupplierIngredientsAsync(id, request.IngredientIds, cancellationToken);
        
        _logger.LogInformation("Updated supplier: {SupplierName} (ID: {SupplierId})", 
            updatedSupplier.SupplierName, updatedSupplier.SupplierId);
        
        return MapToDto(updatedSupplier);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteSupplierAsync(long id, CancellationToken cancellationToken = default)
    {
        var supplier = await _supplierRepository.GetByIdAsync(id, cancellationToken);
        
        if (supplier == null)
        {
            _logger.LogWarning("Supplier with ID {SupplierId} not found", id);
            throw new KeyNotFoundException($"Supplier with ID {id} not found.");
        }

        // Check if supplier has dependencies
        var hasDependencies = await _supplierRepository.HasDependenciesAsync(id, cancellationToken);
        if (hasDependencies)
        {
            _logger.LogWarning("Cannot delete supplier {SupplierId}: has related ingredients or transactions", id);
            throw new InvalidOperationException("Cannot delete supplier because it has related ingredients or inventory transactions.");
        }

        var result = await _supplierRepository.DeleteAsync(id, cancellationToken);
        
        if (result)
        {
            _logger.LogInformation("Deleted supplier: {SupplierName} (ID: {SupplierId})", 
                supplier.SupplierName, id);
        }
        
        return result;
    }

    private static SupplierDto MapToDto(Supplier supplier)
    {
        return new SupplierDto
        {
            SupplierId = supplier.SupplierId,
            SupplierName = supplier.SupplierName,
            Phone = supplier.Phone,
            Email = supplier.Email,
            Address = supplier.Address,
            TaxCode = supplier.TaxCode
        };
    }
}
