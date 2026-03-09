using Core.DTO.General;
using Core.DTO.Supplier;
using Core.Entity;

namespace Core.Interface.Repo;

/// <summary>
/// Repository interface for supplier data access
/// </summary>
public interface ISupplierRepository
{
    /// <summary>
    /// Get paginated suppliers with filtering
    /// </summary>
    /// <param name="query">Query parameters including pagination and filters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated result of suppliers</returns>
    Task<PagedResultDTO<SupplierDto>> GetAllSuppliersAsync(SupplierListQueryDTO query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get supplier by ID
    /// </summary>
    /// <param name="id">Supplier ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Supplier or null if not found</returns>
    Task<Supplier?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get supplier by ID with ingredients
    /// </summary>
    /// <param name="id">Supplier ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Supplier with ingredients or null if not found</returns>
    Task<Supplier?> GetByIdWithIngredientsAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new supplier
    /// </summary>
    /// <param name="supplier">Supplier to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created supplier</returns>
    Task<Supplier> CreateAsync(Supplier supplier, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing supplier
    /// </summary>
    /// <param name="supplier">Supplier to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated supplier</returns>
    Task<Supplier> UpdateAsync(Supplier supplier, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a supplier
    /// </summary>
    /// <param name="id">Supplier ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted successfully, false if not found</returns>
    Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if supplier name already exists
    /// </summary>
    /// <param name="name">Supplier name to check</param>
    /// <param name="excludeId">Supplier ID to exclude from check (for updates)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if name exists, false otherwise</returns>
    Task<bool> ExistsByNameAsync(string name, long? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if supplier has related records (ingredients or transactions)
    /// </summary>
    /// <param name="id">Supplier ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if supplier has dependencies</returns>
    Task<bool> HasDependenciesAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update supplier's ingredient relationships
    /// </summary>
    /// <param name="supplierId">Supplier ID</param>
    /// <param name="ingredientIds">List of ingredient IDs</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdateSupplierIngredientsAsync(long supplierId, List<long> ingredientIds, CancellationToken cancellationToken = default);
}
