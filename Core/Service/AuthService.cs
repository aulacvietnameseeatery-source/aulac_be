using Core.Data;
using Core.DTO.Auth;
using Core.DTO.Email;
using Core.Entity;
using Core.Extensions;
using Core.Interface.Repo;
using Core.Interface.Service.Auth;
using Core.Interface.Service.Email;
using Core.Interface.Service.Entity;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace Core.Service;

/// <summary>
/// Implementation of IAuthService that orchestrates authentication flows.
/// Coordinates between token service, session repository, account repository, and password reset operations.
/// </summary>
public class AuthService : IAuthService
{
    private readonly ITokenService _tokenService;
    private readonly IAuthSessionRepository _sessionRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IPasswordResetTokenStore _tokenStore;
    private readonly IEmailQueue _emailQueue;
    private readonly ILookupResolver _lookupResolver;

    // Password reset security configuration
    private readonly ForgotPasswordRulesOptions _forgotOpt;
    private readonly BaseUrlOptions _baseUrl;


    public AuthService(
        ITokenService tokenService,
        IAuthSessionRepository sessionRepository,
        IAccountRepository accountRepository,
        IPasswordHasher passwordHasher,
        IPasswordResetTokenStore tokenStore,
        IEmailQueue emailQueue,
        ILookupResolver lookupResolver,
        IOptions<ForgotPasswordRulesOptions> forgotOpt,
        IOptions<BaseUrlOptions> baseUrlOpt)
    {
        _tokenService = tokenService;
        _sessionRepository = sessionRepository;
        _accountRepository = accountRepository;
        _passwordHasher = passwordHasher;
        _tokenStore = tokenStore;
        _emailQueue = emailQueue;
        _lookupResolver = lookupResolver;

        _forgotOpt = forgotOpt.Value;
        _baseUrl = baseUrlOpt.Value;
    }


    /// <inheritdoc />
    /// <remarks>
    /// Login Flow:
    /// 1. Validate credentials (username/email + password)
    /// 2. Check account status (not locked)
    /// 3. Generate access token (short-lived JWT)
    /// 4. Generate refresh token (cryptographically random)
    /// 5. Hash refresh token and store session
    /// 6. Update last login timestamp
    /// 7. Return tokens to client
    /// 
    /// Note: The login identifier can be either username or email address.
    /// 
    /// **First-Time Login:**
    /// - If account is LOCKED, password verification still succeeds
    /// - Returns special response indicating password change is required
    /// - Client must redirect to password change page
    /// - Account becomes ACTIVE after password change
    /// </remarks>
    public async Task<AuthResult> LoginAsync(
        LoginRequest request,
        string? deviceInfo = null,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        // Step 1: Find account by username or email
        StaffAccount? account = null;

        // Try to find by username first
        account = await _accountRepository.FindByUsernameAsync(request.Username, cancellationToken);

        // If not found, try to find by email (case-insensitive)
        if (account == null)
        {
            var emailNormalized = request.Username.Trim().ToUpperInvariant();
            account = await _accountRepository.FindByEmailAsync(emailNormalized, cancellationToken);
        }

        if (account == null)
        {
            return AuthResult.Failed("INVALID_CREDENTIALS", "Invalid username or password.");
        }

        // Step 2: Verify password
        if (!_passwordHasher.VerifyPassword(request.Password, account.PasswordHash))
        {
            return AuthResult.Failed("INVALID_CREDENTIALS", "Invalid username or password.");
        }

        // Step 3: Check if account requires password change (LOCKED status)
        // Allow login but flag for required password change
        var requirePasswordChange = account.AccountStatusLvId == await AccountStatusCode.LOCKED.ToAccountStatusIdAsync(_lookupResolver, cancellationToken);

        if (requirePasswordChange)
        {
            // Allow limited session for password change
            // Note: This is a special case - account is locked but we allow login
            // to enable password change
            var tempSession = await _sessionRepository.CreateSessionAsync(
                account.AccountId,
                "temp_for_password_change",
                DateTime.UtcNow.AddMinutes(15), // Short-lived
                deviceInfo,
                ipAddress,
                cancellationToken);

            var roles = new[] { account.Role.RoleCode };
            var permissions = account.Role.Permissions.Select(p => $"{p.ScreenCode}:{p.ActionCode}");

            var tempAccessToken = _tokenService.GenerateAccessToken(
                account.AccountId,
                account.Username,
                tempSession.SessionId,
                roles,
                permissions);

            return AuthResult.PasswordChangeRequired(
                tempAccessToken,
                (int)TimeSpan.FromMinutes(15).TotalSeconds,
                tempSession.SessionId,
                account.AccountId,
                account.Username,
                "Your account requires a password change before you can continue.");
        }

        // Step 4: Normal login flow (account is active)
        // Generate refresh token
        var refreshToken = _tokenService.GenerateRefreshToken();
        var refreshTokenHash = _tokenService.HashToken(refreshToken);
        var refreshTokenExpiry = DateTime.UtcNow.Add(_tokenService.RefreshTokenLifetime);

        // Step 5: Create session with hashed token
        var session = await _sessionRepository.CreateSessionAsync(
            account.AccountId,
            refreshTokenHash,
            refreshTokenExpiry,
            deviceInfo,
            ipAddress,
            cancellationToken);

        // Step 6: Generate access token with session ID
        var sessionRoles = new[] { account.Role.RoleCode };
        var sessionPermissions = account.Role.Permissions.Select(p => $"{p.ScreenCode}:{p.ActionCode}");

        var accessToken = _tokenService.GenerateAccessToken(
            account.AccountId,
            account.Username,
            session.SessionId,
            sessionRoles,
            sessionPermissions);

        // Step 7: Update last login
        await _accountRepository.UpdateLastLoginAsync(account.AccountId, DateTime.UtcNow, cancellationToken);

        return AuthResult.Succeeded(
            accessToken,
            refreshToken,
            (int)_tokenService.AccessTokenLifetime.TotalSeconds,
            session.SessionId,
            account.AccountId,
            account.Username,
            sessionRoles);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Refresh Token Flow:
    /// 1. Extract session ID from expired access token
    /// 2. Validate refresh token format and existence
    /// 3. Verify session: not revoked, not expired
    /// 4. Verify token hash matches
    /// 5. Generate new refresh token (rotation)
    /// 6. Update session with new token hash
    /// 7. Generate new access token
    /// 8. Return new tokens
    /// </remarks>
    public async Task<AuthResult> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        // Step 1: Extract claims from expired access token
        var principal = _tokenService.GetPrincipalFromExpiredToken(request.AccessToken);
        if (principal == null)
        {
            return AuthResult.Failed("INVALID_TOKEN", "Invalid access token.");
        }

        // Step 2: Get session ID from claims
        var sessionIdClaim = principal.FindFirst("session_id");
        var userIdClaim = principal.FindFirst("user_id");

        if (sessionIdClaim == null || !long.TryParse(sessionIdClaim.Value, out var sessionId))
        {
            return AuthResult.Failed("INVALID_TOKEN", "Session information not found in token.");
        }

        if (userIdClaim == null || !long.TryParse(userIdClaim.Value, out var userId))
        {
            return AuthResult.Failed("INVALID_TOKEN", "User information not found in token.");
        }

        // Step 3: Validate refresh token against session
        var refreshTokenHash = _tokenService.HashToken(request.RefreshToken);
        var session = await _sessionRepository.ValidateRefreshTokenAsync(sessionId, refreshTokenHash, cancellationToken);

        if (session == null)
        {
            // Possible token reuse attack - revoke all sessions for this user
            await _sessionRepository.RevokeAllUserSessionsAsync(userId, cancellationToken: cancellationToken);
            return AuthResult.Failed("INVALID_REFRESH_TOKEN", "Invalid or expired refresh token. All sessions have been revoked for security.");
        }

        // Step 4: Get updated user information
        var account = await _accountRepository.FindByIdWithRoleAsync(userId, cancellationToken);
        if (account == null || account.IsLocked)
        {
            await _sessionRepository.RevokeSessionAsync(sessionId, cancellationToken);
            return AuthResult.Failed("ACCOUNT_UNAVAILABLE", "Account not found or has been locked.");
        }

        // Step 5: Generate new refresh token (rotation)
        var newRefreshToken = _tokenService.GenerateRefreshToken();
        var newRefreshTokenHash = _tokenService.HashToken(newRefreshToken);
        var newExpiresAt = DateTime.UtcNow.Add(_tokenService.RefreshTokenLifetime);

        // Step 6: Update session with new token
        await _sessionRepository.RotateRefreshTokenAsync(
      sessionId, newRefreshTokenHash, newExpiresAt, cancellationToken);

        // Step 7: Generate new access token
        var roles = new[] { account.Role.RoleCode };
        var permissions = account.Role.Permissions.Select(p => $"{p.ScreenCode}:{p.ActionCode}");

        var newAccessToken = _tokenService.GenerateAccessToken(
            account.AccountId,
            account.Username,
            sessionId,
            roles,
            permissions);

        return AuthResult.Succeeded(
            newAccessToken,
            newRefreshToken,
            (int)_tokenService.AccessTokenLifetime.TotalSeconds,
            sessionId,
            account.AccountId,
            account.Username,
            roles);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Logout Flow:
    /// 1. Mark session as revoked
    /// 2. Future requests with this session will fail validation
    /// </remarks>
    public async Task<bool> LogoutAsync(long sessionId, CancellationToken cancellationToken = default)
    {
        return await _sessionRepository.RevokeSessionAsync(sessionId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> LogoutAllAsync(
        long userId,
        long? currentSessionId = null,
        CancellationToken cancellationToken = default)
    {
        return await _sessionRepository.RevokeAllUserSessionsAsync(userId, currentSessionId, cancellationToken);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Session Validation (called by JWT middleware):
    /// 1. Load session from database
    /// 2. Check: exists, not revoked, not expired
    /// 3. Return validation result
    /// </remarks>
    public async Task<bool> ValidateSessionAsync(long sessionId, CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetValidSessionAsync(sessionId, cancellationToken);
        return session != null;
    }

    #region Password Reset

    /// <inheritdoc />
    /// <remarks>
    /// Implementation notes:
    /// - Always succeeds to prevent user enumeration
    /// - Token is URL-safe Base64 encoded
    /// - Only the hash is stored, never the plaintext token
    /// - Invalidates any existing reset tokens for the user
    /// </remarks>
    public async Task RequestPasswordResetAsync(string email, string? ip, string? userAgent, CancellationToken cancellationToken = default)
    {
        // Normalize email for case-insensitive lookup
        var emailNormalized = email.Trim().ToUpperInvariant();

        // Find user by email
        var account = await _accountRepository.FindByEmailAsync(emailNormalized, cancellationToken);

        // Security: Always continue processing to prevent timing attacks
        // This ensures response time is consistent whether email exists or not
        if (account == null || account.IsLocked)
        {
            // Simulate processing delay to prevent timing analysis
            await Task.Delay(Random.Shared.Next(50, 150), cancellationToken);
            return; // Silently fail - don't reveal if email exists
        }

        // Generate cryptographically secure random token
        var lifetime = TimeSpan.FromMinutes(_forgotOpt.TokenLifetimeMinutes);
        var token = GenerateSecurePasswordResetToken(_forgotOpt.TokenLengthBytes);
        var tokenHash = HashPasswordResetToken(token);

        // Create token record
        var record = new DTO.Auth.PasswordResetTokenRecord(
            UserId: account.AccountId,
            EmailNormalized: emailNormalized,
            TokenHash: tokenHash,
            ExpiresAt: DateTimeOffset.UtcNow.Add(lifetime),
            IssuedAt: DateTimeOffset.UtcNow
        );

        // Invalidate any existing tokens for this user (only one active reset at a time)
        await _tokenStore.InvalidateUserAsync(account.AccountId, cancellationToken);

        // Store new token in Redis with TTL
        await _tokenStore.StoreAsync(record, lifetime, cancellationToken);

        // Queue password reset email (processed by background worker)
        var resetLink = $"{_baseUrl.Client.TrimEnd('/')}/reset-password?token={token}";
        var emailHtml = BuildPasswordResetEmail(account.Username, resetLink, lifetime);

        await _emailQueue.EnqueueAsync(new QueuedEmail(
                To: account.Email,
                Subject: "Password Reset Request",
                HtmlBody: emailHtml,
                CorrelationId: $"pwdreset:{account.AccountId}:{DateTimeOffset.UtcNow.Ticks}"
                ), cancellationToken);

        // Note: We don't log the token, only metadata for security audit
        // Logging could include: userId, ip, userAgent, timestamp
    }

    /// <inheritdoc />
    public async Task<bool> VerifyPasswordResetTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        var tokenHash = HashPasswordResetToken(token);
        var record = await _tokenStore.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (record == null)
            return false;

        // Check expiration
        if (record.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            // Token expired - clean it up
            await _tokenStore.ConsumeAsync(tokenHash, cancellationToken);
            return false;
        }

        return true;
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">
    /// Thrown when token is invalid, expired, or user account is not found
    /// </exception>
    public async Task ResetPasswordAsync(string token, string newPassword, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("Invalid reset token.");

        if (string.IsNullOrWhiteSpace(newPassword))
            throw new ArgumentException("Password cannot be empty.", nameof(newPassword));

        // Hash token and retrieve record
        var tokenHash = HashPasswordResetToken(token);
        var record = await _tokenStore.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (record == null)
            throw new InvalidOperationException("Invalid or expired reset token.");

        // Verify token hasn't expired
        if (record.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            await _tokenStore.ConsumeAsync(tokenHash, cancellationToken);
            throw new InvalidOperationException("Reset token has expired.");
        }

        // Retrieve user account
        var account = await _accountRepository.FindByIdAsync(record.UserId, cancellationToken);
        if (account == null || account.IsLocked)
        {
            await _tokenStore.ConsumeAsync(tokenHash, cancellationToken);
            throw new InvalidOperationException("User account not found or is locked.");
        }

        // Hash the new password
        var newPasswordHash = _passwordHasher.HashPassword(newPassword);

        // Update password in database
        await _accountRepository.UpdatePasswordAsync(account.AccountId, newPasswordHash, cancellationToken);

        // Consume the token (one-time use)
        await _tokenStore.ConsumeAsync(tokenHash, cancellationToken);

        // Security: Revoke all active sessions (force user to re-login everywhere)
        await _sessionRepository.RevokeAllUserSessionsAsync(record.UserId, exceptSessionId: null, cancellationToken);
    }

    #endregion

    #region Password Reset Helper Methods

    /// <summary>
    /// Generates a cryptographically secure random token for password reset.
    /// </summary>
    /// <returns>URL-safe Base64 encoded token</returns>
    private static string GenerateSecurePasswordResetToken(int tokenLengthBytes)
    {
        var bytes = new byte[tokenLengthBytes];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }

        // URL-safe Base64 encoding
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    /// <summary>
    /// Hashes a password reset token using SHA-256 for storage.
    /// We never store plaintext tokens - only their hashes.
    /// </summary>
    /// <param name="token">The plaintext token</param>
    /// <returns>Hexadecimal representation of the hash</returns>
    private static string HashPasswordResetToken(string token)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    /// <summary>
    /// Builds the HTML email body for password reset.
    /// </summary>
    /// <param name="username">The user's username</param>
    /// <param name="resetLink">The password reset link with embedded token</param>
    /// <param name="expiresIn">Token expiration duration</param>
    /// <returns>HTML email body</returns>
    /// TODO: Change to use a templating
    private static string BuildPasswordResetEmail(string username, string resetLink, TimeSpan expiresIn)
    {
        var expiryMinutes = (int)expiresIn.TotalMinutes;

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
  .button {{ 
     display: inline-block; 
            padding: 12px 24px; 
  background-color: #007bff; 
      color: #ffffff; 
            text-decoration: none; 
            border-radius: 4px; 
        margin: 20px 0;
        }}
  .warning {{ color: #856404; background-color: #fff3cd; padding: 10px; border-radius: 4px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <h2>Password Reset Request</h2>
        <p>Hello {username},</p>
        <p>We received a request to reset your password. Click the button below to create a new password:</p>
        <a href=""{resetLink}"" class=""button"">Reset Password</a>
 <p>Or copy and paste this link into your browser:</p>
        <p><a href=""{resetLink}"">{resetLink}</a></p>
        <div class=""warning"">
      <strong>Security Notice:</strong>
  <ul>
            <li>This link will expire in {expiryMinutes} minutes</li>
                <li>If you didn't request a password reset, you can safely ignore this email</li>
            <li>Never share this link with anyone</li>
    </ul>
        </div>
      <p>Best regards,<br>Your Application Team</p>
    </div>
</body>
</html>
";
    }

    #endregion
}
