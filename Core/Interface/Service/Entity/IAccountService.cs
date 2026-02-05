
using Core.DTO.Account;
using Core.DTO.Auth;

namespace Core.Interface.Service.Entity;

/// <summary>
/// Service abstraction for account management operations.
/// Provides business logic for account-related operations beyond authentication.
/// </summary>
public interface IAccountService
{
    /// <summary>
    /// Creates a new account with system-generated credentials.
    /// </summary>
    /// <param name="request">Account creation details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created account result with username and status</returns>
    Task<CreateAccountResult> CreateAccountAsync(
        CreateAccountRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates account profile information (excluding password).
    /// </summary>
    /// <param name="accountId">Account ID to update</param>
    /// <param name="request">Updated account details</param>
    /// <param name="requestingUserId">User making the update (for authorization)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<AccountDetailDto> UpdateAccountAsync(
        long accountId,
        UpdateAccountRequest request,
        long requestingUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets detailed account information with status names resolved.
    /// </summary>
    Task<AccountDetailDto?> GetAccountDetailAsync(
        long accountId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets an account's password to the system default password.
    /// Account becomes LOCKED and requires password change on next login.
    /// </summary>
    /// <param name="accountId">The account ID to reset</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing success status and account information</returns>
    Task<PasswordResetResult> ResetToDefaultPasswordAsync(
        long accountId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes an account's password and activates account if it was locked.
    /// This is an internal method - use ChangePasswordForSelfAsync for user-initiated changes.
    /// </summary>
    /// <param name="accountId">The account ID</param>
    /// <param name="newPassword">The new password</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ChangePasswordAsync(
        long accountId,
        string newPassword,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes user's own password with current password verification.
    /// Handles both first-time password change (locked accounts) and normal password change.
    /// </summary>
    /// <param name="accountId">The account ID</param>
    /// <param name="currentPassword">Current password (optional for first-time change)</param>
    /// <param name="newPassword">New password</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if password changed successfully</returns>
    Task<bool> ChangePasswordForSelfAsync(
        long accountId,
        string? currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets account details by ID (legacy method - use GetAccountDetailAsync for new code).
    /// </summary>
    /// <param name="accountId">The account ID</param>
    /// <param name="includeRole">Whether to include role and permissions</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Account information or null if not found</returns>
    Task<AccountDto?> GetAccountByIdAsync(
        long accountId,
        bool includeRole = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates if an account exists and is active.
    /// </summary>
    /// <param name="accountId">The account ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if account exists and is active; otherwise false</returns>
    Task<bool> IsAccountActiveAsync(
        long accountId,
        CancellationToken cancellationToken = default);
}


