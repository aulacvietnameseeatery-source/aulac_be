using Core.Entity;

namespace Core.Interface.Repo;

/// <summary>
/// Account repository abstraction for authentication purposes.
/// Provides read-only access to account data needed for authentication.
/// </summary>
public interface IAccountRepository
{
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
}
