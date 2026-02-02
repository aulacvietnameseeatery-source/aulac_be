using Core.Data;
using Core.Enum;
using Core.Extensions;
using Core.Interface.Repo;
using Core.Interface.Service.Auth;
using Core.Interface.Service.Entity;
using Microsoft.Extensions.Logging;

namespace Core.Service;

/// <summary>
/// Service implementation for account management operations.
/// Handles business logic for account-related operations.
/// </summary>
public class AccountService : IAccountService
{
    private readonly IAccountRepository _accountRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ISystemSettingService _systemSettingService;
    private readonly ILookupResolver _lookupResolver;
    private readonly ILogger<AccountService> _logger;

    public AccountService(
        IAccountRepository accountRepository,
        IPasswordHasher passwordHasher,
        ISystemSettingService systemSettingService,
        ILookupResolver lookupResolver,
        ILogger<AccountService> logger)
    {
        _accountRepository = accountRepository;
        _passwordHasher = passwordHasher;
        _systemSettingService = systemSettingService;
        _lookupResolver = lookupResolver;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<PasswordResetResult> ResetToDefaultPasswordAsync(
        long accountId,
        CancellationToken cancellationToken = default)
    {
        // Retrieve the default password from system settings
        var defaultPassword = await _systemSettingService.GetStringAsync(
            "default_password",
            cancellationToken: cancellationToken);

        if (string.IsNullOrWhiteSpace(defaultPassword))
        {
            _logger.LogError("Default password setting not configured in system settings");
            throw new InvalidOperationException("Default password is not configured in the system. Please contact your administrator.");
        }

        // Find the account by ID
        var account = await _accountRepository.FindByIdAsync(accountId, cancellationToken);

        if (account == null)
        {
            _logger.LogWarning("Password reset failed: Account with ID {AccountId} not found", accountId);
            throw new KeyNotFoundException($"Account with ID {accountId} not found.");
        }

        // Hash the default password
        var hashedPassword = _passwordHasher.HashPassword(defaultPassword);

        // Update the password
        await _accountRepository.UpdatePasswordAsync(accountId, hashedPassword, cancellationToken);

        _logger.LogInformation(
            "Password reset to default for account ID {AccountId} (Username: {Username})",
            accountId,
            account.Username);

        return new PasswordResetResult
        {
            AccountId = account.AccountId,
            Username = account.Username,
            FullName = account.FullName,
            Success = true,
            Message = "Password has been reset to the default password"
        };
    }

    /// <inheritdoc />
    public async Task ChangePasswordAsync(
        long accountId,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        // Validate password
        if (string.IsNullOrWhiteSpace(newPassword))
        {
            throw new ArgumentException("Password cannot be empty", nameof(newPassword));
        }

        if (newPassword.Length < 8)
        {
            throw new ArgumentException("Password must be at least 8 characters", nameof(newPassword));
        }

        // Find the account
        var account = await _accountRepository.FindByIdAsync(accountId, cancellationToken);

        if (account == null)
        {
            throw new KeyNotFoundException($"Account with ID {accountId} not found.");
        }

        // Hash and update password
        var hashedPassword = _passwordHasher.HashPassword(newPassword);
        await _accountRepository.UpdatePasswordAsync(accountId, hashedPassword, cancellationToken);

        _logger.LogInformation(
            "Password changed for account ID {AccountId} (Username: {Username})",
            accountId,
            account.Username);
    }

    /// <inheritdoc />
    public async Task<AccountDto?> GetAccountByIdAsync(
        long accountId,
        bool includeRole = false,
        CancellationToken cancellationToken = default)
    {
        var account = includeRole
            ? await _accountRepository.FindByIdWithRoleAsync(accountId, cancellationToken)
            : await _accountRepository.FindByIdAsync(accountId, cancellationToken);

        if (account == null)
        {
            return null;
        }

        return new AccountDto
        {
            AccountId = account.AccountId,
            Username = account.Username,
            FullName = account.FullName,
            Email = account.Email,
            Phone = account.Phone,
            AccountStatus = (int)account.AccountStatusLvId,
            IsLocked = account.IsLocked,
            CreatedAt = account.CreatedAt,
            LastLoginAt = account.LastLoginAt,
            Role = includeRole && account.Role != null
                ? new RoleDto
                {
                    RoleId = account.Role.RoleId,
                    RoleName = account.Role.RoleName,
                    RoleCode = account.Role.RoleCode
                }
                : null
        };
    }

    /// <inheritdoc />
    public async Task<bool> IsAccountActiveAsync(
        long accountId,
        CancellationToken cancellationToken = default)
    {
        var account = await _accountRepository.FindByIdAsync(accountId, cancellationToken);

        if (account == null)
        {
            return false;
        }

        // Use type-specific async extension method for cleanest syntax
        var activeStatusId = await AccountStatusCode.ACTIVE.ToAccountStatusIdAsync(_lookupResolver, cancellationToken);

        // Check if account status matches ACTIVE and is not locked
        return account.AccountStatusLvId == activeStatusId && !account.IsLocked;
    }
}
