using Core.Entity;

namespace Core.Interface.Repo;

/// <summary>
/// Repository interface for role-related database operations.
/// </summary>
public interface IRoleRepository
{
    /// <summary>
    /// Finds a role by its ID.
    /// </summary>
    /// <param name="roleId">The role ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The role entity or null if not found</returns>
    Task<Role?> FindByIdAsync(long roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a role by its code.
    /// </summary>
    /// <param name="roleCode">The role code (e.g., "ADMIN", "MANAGER")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The role entity or null if not found</returns>
    Task<Role?> FindByCodeAsync(string roleCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active roles.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all roles</returns>
    Task<List<Role>> GetAllAsync(CancellationToken cancellationToken = default);
}
