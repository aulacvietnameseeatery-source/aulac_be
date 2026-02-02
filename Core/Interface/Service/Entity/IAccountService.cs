using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interface.Service.Entity;

/// <summary>
/// Service abstraction for account management operations.
/// Provides business logic for account-related operations beyond authentication.
/// </summary>
public interface IAccountService
{
    /// <summary>
    /// Resets an account's password to the system default password.
    /// </summary>
    /// <param name="accountId">The account ID to reset</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing success status and account information</returns>
    /// <exception cref="InvalidOperationException">Thrown when default password is not configured</exception>
    /// <exception cref="KeyNotFoundException">Thrown when account is not found</exception>
    Task<PasswordResetResult> ResetToDefaultPasswordAsync(
        long accountId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes an account's password.
    /// </summary>
    /// <param name="accountId">The account ID</param>
    /// <param name="newPassword">The new password</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ChangePasswordAsync(
        long accountId,
        string newPassword,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets account details by ID.
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

/// <summary>
/// Result of a password reset operation.
/// </summary>
public record PasswordResetResult
{
    public long AccountId { get; init; }
    public string Username { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public bool Success { get; init; }
    public string? Message { get; init; }
}

/// <summary>
/// Account data transfer object.
/// </summary>
public record AccountDto
{
    public long AccountId { get; init; }
    public string Username { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public int AccountStatus { get; init; }
    public bool IsLocked { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? LastLoginAt { get; init; }
    public RoleDto? Role { get; init; }
}

/// <summary>
/// Role data transfer object.
/// </summary>
public record RoleDto
{
    public long RoleId { get; init; }
    public string RoleName { get; init; } = string.Empty;
    public string RoleCode { get; init; } = string.Empty;
}
