using Core.Interface.Repo;
using Core.Interface.Service;

namespace Core.Service;

/// <summary>
/// Implementation of IAuthService that orchestrates authentication flows.
/// Coordinates between token service, session repository, and account repository.
/// </summary>
public class AuthService : IAuthService
{
    private readonly ITokenService _tokenService;
    private readonly IAuthSessionRepository _sessionRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly IPasswordHasher _passwordHasher;

    public AuthService(
        ITokenService tokenService,
        IAuthSessionRepository sessionRepository,
        IAccountRepository accountRepository,
        IPasswordHasher passwordHasher)
    {
        _tokenService = tokenService;
        _sessionRepository = sessionRepository;
        _accountRepository = accountRepository;
        _passwordHasher = passwordHasher;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Login Flow:
    /// 1. Validate credentials (username + password)
    /// 2. Check account status (not locked)
    /// 3. Generate access token (short-lived JWT)
    /// 4. Generate refresh token (cryptographically random)
    /// 5. Hash refresh token and store session
    /// 6. Update last login timestamp
    /// 7. Return tokens to client
    /// </remarks>
    public async Task<AuthResult> LoginAsync(
        LoginRequest request,
        string? deviceInfo = null,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        // Step 1: Find account by username
        var account = await _accountRepository.FindByUsernameAsync(
       request.Username, cancellationToken);

        if (account == null)
        {
            return AuthResult.Failed("INVALID_CREDENTIALS", "Invalid username or password.");
        }

        // Step 2: Verify password
        if (!_passwordHasher.VerifyPassword(request.Password, account.PasswordHash))
        {
            return AuthResult.Failed("INVALID_CREDENTIALS", "Invalid username or password.");
        }

        // Step 3: Check account status
        if (account.IsLocked)
        {
            return AuthResult.Failed("ACCOUNT_LOCKED", "This account has been locked.");
        }

        // Step 4: Generate refresh token
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
        var roles = new[] { account.Role.RoleCode };
        var permissions = account.Role.Permissions.Select(p => $"{p.ScreenCode}:{p.ActionCode}");

        var accessToken = _tokenService.GenerateAccessToken(
            account.AccountId,
            account.Username,
            session.SessionId,
            roles,
            permissions);

        // Step 7: Update last login
        await _accountRepository.UpdateLastLoginAsync(account.AccountId, DateTime.UtcNow, cancellationToken);

        return AuthResult.Succeeded(
            accessToken,
            refreshToken,
            (int)_tokenService.AccessTokenLifetime.TotalSeconds,
            session.SessionId,
            account.AccountId,
            account.Username,
            roles);
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
    public async Task<AuthResult> RefreshTokenAsync(RefreshTokenRequest request,CancellationToken cancellationToken = default)
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
        var session = await _sessionRepository.ValidateRefreshTokenAsync(
                 sessionId, refreshTokenHash, cancellationToken);

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
    public async Task<bool> LogoutAsync(
  long sessionId,
        CancellationToken cancellationToken = default)
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
    public async Task<bool> ValidateSessionAsync(long sessionId,CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetValidSessionAsync(sessionId, cancellationToken);
        return session != null;
    }
}
