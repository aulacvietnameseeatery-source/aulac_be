using Core.DTO.Account;
using Core.DTO.General;
using Core.Entity;

namespace Core.Interface.Repo;

/// <summary>
/// Repository interface for account-related database operations.
/// </summary>
public interface IAccountRepository
{
    /// <summary>
    /// Creates a new staff account.
    /// </summary>
    /// <param name="account">The account entity to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created account with generated ID</returns>
    Task<StaffAccount> CreateAsync(
        StaffAccount account,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an email already exists (case-insensitive).
    /// </summary>
    /// <param name="emailNormalized">The uppercase email to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if email exists, false otherwise</returns>
    Task<bool> EmailExistsAsync(
        string emailNormalized,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a username already exists.
    /// </summary>
    /// <param name="username">The username to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if username exists, false otherwise</returns>
    Task<bool> UsernameExistsAsync(
        string username,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an account entity.
    /// </summary>
    /// <param name="account">The account to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdateAccountAsync(
        StaffAccount account,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds an account by username.
    /// </summary>
    /// <param name="username">The username to search for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The account if found; otherwise null</returns>
    Task<StaffAccount?> FindByUsernameAsync(
        string username,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds an account by ID with role and permissions loaded.
    /// </summary>
    /// <param name="userId">The user's identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The account with role/permissions if found; otherwise null</returns>
    Task<StaffAccount?> FindByIdWithRoleAsync(
        long userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds an account by ID without loading related entities.
    /// </summary>
    /// <param name="userId">The user's identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The account if found; otherwise null</returns>
    Task<StaffAccount?> FindByIdAsync(
        long userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds an account by email address (case-insensitive).
    /// </summary>
    /// <param name="emailNormalized">The normalized (uppercase) email to search for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The account if found; otherwise null</returns>
    Task<StaffAccount?> FindByEmailAsync(
        string emailNormalized,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the last login timestamp for an account.
    /// </summary>
    /// <param name="userId">The user's identifier</param>
    /// <param name="loginTime">The login timestamp</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdateLastLoginAsync(
        long userId,
        DateTime loginTime,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the password hash for an account.
    /// </summary>
    /// <param name="userId">The user's identifier</param>
    /// <param name="newPasswordHash">The new hashed password</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdatePasswordAsync(
        long userId,
        string newPasswordHash,
        CancellationToken cancellationToken = default);

	Task<PagedResultDTO<AccountListDTO>> GetAccountsAsync(
	AccountListQueryDTO query,
	CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets all active roles for account management.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>List of roles</returns>
	Task<List<RoleDTO>> GetAllRolesAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets all account statuses from lookup values.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>List of account statuses</returns>
	Task<List<AccountStatusDTO>> GetAccountStatusesAsync(CancellationToken cancellationToken = default);

}
