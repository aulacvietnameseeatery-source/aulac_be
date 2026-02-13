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

    /// <summary>
    /// Gets a paged list of roles with the count of staff accounts per role.
    /// </summary>
    /// <param name="pageIndex">The page index (zero-based)</param>
    /// <param name="pageSize">The number of items per page</param>
    /// <param name="search">Optional search term for filtering roles</param>
    /// <returns>A tuple containing the list of roles and the total count</returns>
    /// <returns>A tuple containing the list of roles and the total count</returns>
    Task<(List<Role> Roles, int TotalCount)> GetPagedWithStaffCountAsync(int pageIndex, int pageSize, string? search);

    /// <summary>
    /// Deletes a role.
    /// </summary>
    /// <param name="role">The role entity to delete</param>
    /// <returns>Task</returns>
    Task DeleteAsync(Role role);

    Task UpdateAsync(Role role);

    Task<uint?> GetRoleStatusIdAsync(string statusCode);

    /// <summary>
    /// Checks if a role has any staff accounts assigned.
    /// </summary>
    /// <param name="roleId">The role ID</param>
    /// <returns>True if any staff account has this role, false otherwise</returns>
    Task<bool> HasStaffAssignedAsync(long roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a role by ID with its permissions included.
    /// </summary>
    /// <param name="roleId">The role ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The role entity with permissions or null if not found</returns>
    Task<Role?> GetRoleWithPermissionsAsync(long roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a role by ID with its permissions included, with change tracking enabled for updates.
    /// </summary>
    /// <param name="roleId">The role ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The tracked role entity with permissions or null if not found</returns>
    Task<Role?> GetRoleWithPermissionsForUpdateAsync(long roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all available permissions in the system.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all permissions</returns>
    Task<List<Permission>> GetAllPermissionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets permissions by their IDs with tracking enabled.
    /// </summary>
    /// <param name="permissionIds">List of permission IDs to retrieve</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of tracked permissions</returns>
    Task<List<Permission>> GetPermissionsByIdsAsync(List<long> permissionIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new role to the database.
    /// </summary>
    /// <param name="role">The role entity to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The added role with generated ID</returns>
    Task<Role> AddAsync(Role role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a role code already exists.
    /// </summary>
    /// <param name="roleCode">The role code to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if role code exists, false otherwise</returns>
    Task<bool> RoleCodeExistsAsync(string roleCode, CancellationToken cancellationToken = default);


}
