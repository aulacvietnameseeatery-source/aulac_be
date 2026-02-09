
using Core.DTO.Email;
using Core.DTO.Auth;
using Core.Entity;
using Core.Extensions;
using Core.Interface.Repo;
using Core.Interface.Service;
using Core.Interface.Service.Auth;
using Core.Interface.Service.Email;
using Core.Interface.Service.Entity;
using Microsoft.Extensions.Logging;
using Core.Exceptions;
using Core.DTO.Account;

namespace Core.Service;

/// <summary>
/// Service implementation for account management operations.
/// Handles business logic for account-related operations.
/// </summary>
public class AccountService : IAccountService
{
    private readonly IAccountRepository _accountRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IPasswordGenerator _passwordGenerator;
    private readonly IUsernameGenerator _usernameGenerator;
    private readonly ISystemSettingService _systemSettingService;
    private readonly ILookupResolver _lookupResolver;
    private readonly IEmailQueue _emailQueue;
    private readonly ILogger<AccountService> _logger;

    public AccountService(
        IAccountRepository accountRepository,
        IRoleRepository roleRepository,
        IPasswordHasher passwordHasher,
        IPasswordGenerator passwordGenerator,
        IUsernameGenerator usernameGenerator,
        ISystemSettingService systemSettingService,
        ILookupResolver lookupResolver,
        IEmailQueue emailQueue,
        ILogger<AccountService> logger)
    {
        _accountRepository = accountRepository;
        _roleRepository = roleRepository;
        _passwordHasher = passwordHasher;
        _passwordGenerator = passwordGenerator;
        _usernameGenerator = usernameGenerator;
        _systemSettingService = systemSettingService;
        _lookupResolver = lookupResolver;
        _emailQueue = emailQueue;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<CreateAccountResult> CreateAccountAsync(
        CreateAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        // 1. Validate email uniqueness
        var emailNormalized = request.Email.ToUpperInvariant();
        if (await _accountRepository.EmailExistsAsync(emailNormalized, cancellationToken))
        {
            throw new ConflictException($"Email {request.Email} is already registered.");
        }

        // 2. Validate role exists
        var role = await _roleRepository.FindByIdAsync(request.RoleId, cancellationToken);
        if (role == null)
        {
            throw new NotFoundException($"Role ID {request.RoleId} not found.");
        }

        // 3. Generate unique username
        var username = await _usernameGenerator.GenerateUniqueUsernameAsync(
         request.FullName,
         cancellationToken);

        // 4. Generate temporary password
        var temporaryPassword = _passwordGenerator.GenerateTemporaryPassword();
        var hashedPassword = _passwordHasher.HashPassword(temporaryPassword);

        // 5. Get LOCKED status ID
        var lockedStatusId = await AccountStatusCode.LOCKED
         .ToAccountStatusIdAsync(_lookupResolver, cancellationToken);

        // 6. Create account entity
        var account = new StaffAccount
        {
            Username = username,
            Email = request.Email,
            FullName = request.FullName,
            Phone = request.Phone,
            RoleId = request.RoleId,
            PasswordHash = hashedPassword,
            AccountStatusLvId = lockedStatusId,
            IsLocked = true,
            CreatedAt = DateTime.UtcNow
        };

        // 7. Persist to database
        account = await _accountRepository.CreateAsync(account, cancellationToken);

        // 8. Send email (async, don't fail if email fails)
        var emailSent = false;
        try
        {
            var emailHtml = BuildTemporaryPasswordEmail(request.FullName, username, temporaryPassword);
            await _emailQueue.EnqueueAsync(new QueuedEmail(
                To: request.Email,
                Subject: "Your New Account - Temporary Password",
                HtmlBody: emailHtml,
                CorrelationId: $"account_created:{account.AccountId}:{DateTimeOffset.UtcNow.Ticks}"
            ), cancellationToken);

            emailSent = true;
            _logger.LogInformation(
                "Account created successfully. ID: {AccountId}, Username: {Username}, Email queued",
                account.AccountId,
                username);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to queue temporary password email for {Email}", request.Email);
            // Continue - account created successfully even if email fails
        }

        // 9. Return result
        return new CreateAccountResult
        {
            AccountId = account.AccountId,
            Username = username,
            Email = account.Email!,
            FullName = account.FullName,
            AccountStatus = "LOCKED",
            TemporaryPasswordSent = emailSent,
            Message = emailSent
            ? "Account created successfully. Temporary password sent to email."
            : "Account created successfully. Email delivery pending - please check shortly."
        };
    }

    /// <inheritdoc />
    public async Task<AccountDetailDto> UpdateAccountAsync(
        long accountId,
        UpdateAccountRequest request,
        long requestingUserId,
        CancellationToken cancellationToken = default)
    {
        // 1. Find account with role
        var account = await _accountRepository.FindByIdWithRoleAsync(accountId, cancellationToken);
        if (account == null)
        {
            throw new NotFoundException($"Account ID {accountId} not found.");
        }

        var hasChanges = false;

        // 2. Update email (if provided and different)
        if (request.Email != null && !string.Equals(account.Email, request.Email, StringComparison.OrdinalIgnoreCase))
        {
            var emailNormalized = request.Email.ToUpperInvariant();
            if (await _accountRepository.EmailExistsAsync(emailNormalized, cancellationToken))
            {
                throw new ConflictException($"Email {request.Email} is already registered.");
            }
            account.Email = request.Email;
            hasChanges = true;
        }

        // 3. Update full name (if provided and different)
        if (request.FullName != null && account.FullName != request.FullName)
        {
            account.FullName = request.FullName;
            hasChanges = true;
        }

        // 4. Update phone (if provided, including clearing with empty string)
        if (request.Phone != account.Phone)
        {
            account.Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone;
            hasChanges = true;
        }

        // 5. Authorization check for role change
        if (request.RoleId.HasValue && request.RoleId.Value != account.RoleId)
        {
            // Check if requesting user is admin
            var requestingUser = await _accountRepository.FindByIdWithRoleAsync(requestingUserId, cancellationToken);
            if (requestingUser?.Role?.RoleCode != "ADMIN")
            {
                throw new ForbiddenException("Only administrators can change user roles.");
            }

            // Validate new role exists
            var newRole = await _roleRepository.FindByIdAsync(request.RoleId.Value, cancellationToken);
            if (newRole == null)
            {
                throw new NotFoundException($"Role ID {request.RoleId.Value} not found.");
            }

            account.RoleId = request.RoleId.Value;
            hasChanges = true;
        }

        // 6. Persist changes if any
        if (hasChanges)
        {
            await _accountRepository.UpdateAccountAsync(account, cancellationToken);
            _logger.LogInformation(
                "Account updated. ID: {AccountId}, UpdatedBy: {RequestingUserId}",
                accountId,
                requestingUserId);
        }

        // 7. Return updated details
        return await GetAccountDetailAsync(accountId, cancellationToken)
                 ?? throw new InvalidOperationException("Failed to retrieve updated account.");
    }

    /// <inheritdoc />
    public async Task<AccountDetailDto?> GetAccountDetailAsync(
        long accountId,
        CancellationToken cancellationToken = default)
    {
        var account = await _accountRepository.FindByIdWithRoleAsync(accountId, cancellationToken);
        if (account == null)
        {
            return null;
        }

        // For now, just use the ENUM value code directly since we know the pattern
        // A better solution would be to add a reverse lookup method to ILookupResolver
        var statusCode = await GetAccountStatusCodeAsync(account.AccountStatusLvId, cancellationToken);

        return new AccountDetailDto
        {
            AccountId = account.AccountId,
            Username = account.Username,
            FullName = account.FullName,
            Email = account.Email,
            Phone = account.Phone,
            AccountStatus = statusCode,
            IsLocked = account.IsLocked,
            CreatedAt = account.CreatedAt,
            LastLoginAt = account.LastLoginAt,
            UpdatedAt = null, // Not tracked in current schema
            Role = account.Role != null
                ? new RoleDto
                {
                    RoleId = account.Role.RoleId,
                    RoleName = account.Role.RoleName,
                    RoleCode = account.Role.RoleCode
                }
             : null
        };
    }

    /// <summary>
    /// Gets the account status code string from the lookup value ID.
    /// This is a temporary solution until we add reverse lookup to ILookupResolver.
    /// </summary>
    private async Task<string> GetAccountStatusCodeAsync(uint statusLvId, CancellationToken cancellationToken)
    {
        // Check against known status IDs
        var activeId = await AccountStatusCode.ACTIVE.ToAccountStatusIdAsync(_lookupResolver, cancellationToken);
        var lockedId = await AccountStatusCode.LOCKED.ToAccountStatusIdAsync(_lookupResolver, cancellationToken);

        if (statusLvId == activeId)
            return "ACTIVE";
        if (statusLvId == lockedId)
            return "LOCKED";

        // Fallback for unknown statuses
        return "UNKNOWN";
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
            throw new NotFoundException($"Account with ID {accountId} not found.");
        }

        // Hash the default password
        var hashedPassword = _passwordHasher.HashPassword(defaultPassword);

        // Get LOCKED status ID
        var lockedStatusId = await AccountStatusCode.LOCKED
         .ToAccountStatusIdAsync(_lookupResolver, cancellationToken);

        // Update password and lock account
        account.PasswordHash = hashedPassword;
        account.AccountStatusLvId = lockedStatusId;
        account.IsLocked = true;

        await _accountRepository.UpdateAccountAsync(account, cancellationToken);

        _logger.LogInformation(
       "Password reset to default for account ID {AccountId} (Username: {Username}). Account is now LOCKED.",
        accountId,
             account.Username);

        return new PasswordResetResult
        {
            AccountId = account.AccountId,
            Username = account.Username,
            FullName = account.FullName,
            Success = true,
            Message = "Password has been reset to the default password. Account is locked and requires password change on next login."
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
            throw new NotFoundException($"Account with ID {accountId} not found.");
        }

        // Hash and update password
        var hashedPassword = _passwordHasher.HashPassword(newPassword);
        account.PasswordHash = hashedPassword;

        // If account was locked, activate it now
        if (account.IsLocked)
        {
            var activeStatusId = await AccountStatusCode.ACTIVE
         .ToAccountStatusIdAsync(_lookupResolver, cancellationToken);

            account.AccountStatusLvId = activeStatusId;
            account.IsLocked = false;

            _logger.LogInformation(
                "Password changed for account ID {AccountId} (Username: {Username}). Account activated.",
                accountId,
                account.Username);
        }
        else
        {
            _logger.LogInformation(
                "Password changed for account ID {AccountId} (Username: {Username})",
                accountId,
                account.Username);
        }

        await _accountRepository.UpdateAccountAsync(account, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ChangePasswordForSelfAsync(
        long accountId,
        string? currentPassword,
        string newPassword,
CancellationToken cancellationToken = default)
    {
      // Validate new password
   if (string.IsNullOrWhiteSpace(newPassword))
        {
            throw new ValidationException("New password cannot be empty");
        }

        if (newPassword.Length < 8)
        {
    throw new ValidationException("Password must be at least 8 characters");
 }

        // Find the account with role info
   var account = await _accountRepository.FindByIdAsync(accountId, cancellationToken);

     if (account == null)
        {
       throw new NotFoundException($"Account with ID {accountId} not found.");
        }

        // Check if this is a first-time password change (locked account)
        var isFirstTimeChange = account.IsLocked;

  if (!isFirstTimeChange)
   {
          // Normal password change - require current password verification
          if (string.IsNullOrWhiteSpace(currentPassword))
         {
                throw new ValidationException("Current password is required");
            }

            // Verify current password
        if (!_passwordHasher.VerifyPassword(currentPassword, account.PasswordHash))
       {
                _logger.LogWarning(
         "Failed password change attempt for account ID {AccountId} - incorrect current password",
          accountId);
      throw new ValidationException("Current password is incorrect");
       }
    }

     // Hash and update password
        var hashedPassword = _passwordHasher.HashPassword(newPassword);
        account.PasswordHash = hashedPassword;

        // If account was locked (first-time change), activate it
        if (isFirstTimeChange)
{
       var activeStatusId = await AccountStatusCode.ACTIVE
              .ToAccountStatusIdAsync(_lookupResolver, cancellationToken);
    
   account.AccountStatusLvId = activeStatusId;
            account.IsLocked = false;
        
         _logger.LogInformation(
       "First-time password changed for account ID {AccountId} (Username: {Username}). Account activated.",
    accountId,
         account.Username);
        }
else
   {
      _logger.LogInformation(
                "Password changed for account ID {AccountId} (Username: {Username})",
      accountId,
 account.Username);
        }

        await _accountRepository.UpdateAccountAsync(account, cancellationToken);
        
        return true;
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
    public async Task<bool> IsAccountActiveAsync(long accountId,CancellationToken cancellationToken = default)
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

    #region Helper Methods

    /// <summary>
    /// Builds the HTML email body for temporary password.
    /// </summary>
    private static string BuildTemporaryPasswordEmail(string fullName, string username, string temporaryPassword)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .credentials {{ background-color: #f8f9fa; padding: 15px; border-left: 4px solid #007bff; margin: 20px 0; }}
.warning {{ color: #856404; background-color: #fff3cd; padding: 10px; border-radius: 4px; margin: 20px 0; }}
        .code {{ font-family: 'Courier New', monospace; font-size: 16px; font-weight: bold; color: #007bff; }}
    </style>
</head>
<body>
    <div class=""container"">
<h2>Welcome! Your Account Has Been Created</h2>
   <p>Hello {fullName},</p>
        <p>Your account has been successfully created. Here are your login credentials:</p>
 
        <div class=""credentials"">
            <p><strong>Username:</strong> <span class=""code"">{username}</span></p>
  <p><strong>Temporary Password:</strong> <span class=""code"">{temporaryPassword}</span></p>
        </div>

        <div class=""warning"">
     <strong>Important Security Notice:</strong>
 <ul>
      <li>This is a temporary password that must be changed on your first login</li>
  <li>Your account is currently locked and will be activated after you change your password</li>
         <li>Never share your password with anyone</li>
          <li>Keep this email secure and delete it after changing your password</li>
        </ul>
        </div>

        <p>If you did not request this account, please contact your system administrator immediately.</p>
        
    <p>Best regards,<br>Restaurant Management Team</p>
    </div>
</body>
</html>
";
    }

    #endregion
}
