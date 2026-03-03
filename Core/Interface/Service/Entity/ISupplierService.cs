using Core.DTO.General;
using Core.DTO.Supplier;

namespace Core.Interface.Service.Entity;

/// <summary>
/// Service interface for supplier-related business logic
/// </summary>
public interface ISupplierService
{
    /// <summary>
    /// Get paginated suppliers with filtering
    /// </summary>
    /// <param name="query">Query parameters including pagination and filters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated result of suppliers</returns>
    Task<PagedResultDTO<SupplierDto>> GetAllSuppliersAsync(SupplierListQueryDTO query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new supplier
    /// </summary>
    /// <param name="request">Create supplier request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created supplier</returns>
    Task<SupplierDto> CreateSupplierAsync(CreateSupplierRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing supplier
    /// </summary>
    /// <param name="id">Supplier ID</param>
    /// <param name="request">Update supplier request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated supplier</returns>
    /// <exception cref="KeyNotFoundException">Thrown when supplier not found</exception>
    Task<SupplierDto> UpdateSupplierAsync(long id, UpdateSupplierRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a supplier
    /// </summary>
    /// <param name="id">Supplier ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted successfully</returns>
    /// <exception cref="KeyNotFoundException">Thrown when supplier not found</exception>
    /// <exception cref="InvalidOperationException">Thrown when supplier has dependencies</exception>
    Task<bool> DeleteSupplierAsync(long id, CancellationToken cancellationToken = default);
}
