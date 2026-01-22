namespace Core.Interface.Service;

/// <summary>
/// Authentication service abstraction for handling login, refresh, and logout flows.
/// This interface orchestrates token and session operations.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Authenticates a user and creates a new session.
    /// </summary>
    /// <param name="request">Login request containing credentials</param>
    /// <param name="deviceInfo">Optional device/client information</param>
    /// <param name="ipAddress">Optional client IP address</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authentication result containing tokens</returns>
    Task<AuthResult> LoginAsync(
        LoginRequest request,
        string? deviceInfo = null,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes an access token using a valid refresh token.
    /// Implements token rotation for security.
    /// </summary>
    /// <param name="request">Refresh request containing the refresh token</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authentication result containing new tokens</returns>
    Task<AuthResult> RefreshTokenAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs out the current session (revokes the session).
    /// </summary>
    /// <param name="sessionId">The session to revoke</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if logout succeeded</returns>
    Task<bool> LogoutAsync(
        long sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs out all sessions for a user except the current one.
    /// </summary>
    /// <param name="userId">The user's identifier</param>
    /// <param name="currentSessionId">The current session to keep active</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of sessions revoked</returns>
    Task<int> LogoutAllAsync(
        long userId,
        long? currentSessionId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that a session is still valid (not revoked, not expired).
    /// Used by JWT validation middleware.
    /// </summary>
    /// <param name="sessionId">The session to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if session is valid</returns>
    Task<bool> ValidateSessionAsync(
        long sessionId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Login request DTO.
/// </summary>
public record LoginRequest(
    string Username,
    string Password);

/// <summary>
/// Refresh token request DTO.
/// </summary>
public record RefreshTokenRequest(
    string AccessToken,
    string RefreshToken);

/// <summary>
/// Authentication result containing tokens and metadata.
/// </summary>
public record AuthResult
{
    public bool Success { get; init; }
    public string? AccessToken { get; init; }
    public string? RefreshToken { get; init; }
    public int ExpiresIn { get; init; }
    public long? SessionId { get; init; }
    public long? UserId { get; init; }
    public string? Username { get; init; }
    public IEnumerable<string>? Roles { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }

    public static AuthResult Failed(string errorCode, string errorMessage) =>
    new() { Success = false, ErrorCode = errorCode, ErrorMessage = errorMessage };

    public static AuthResult Succeeded(
        string accessToken,
        string refreshToken,
        int expiresIn,
        long sessionId,
        long userId,
        string username,
        IEnumerable<string> roles) =>
     new()
     {
         Success = true,
         AccessToken = accessToken,
         RefreshToken = refreshToken,
         ExpiresIn = expiresIn,
         SessionId = sessionId,
         UserId = userId,
         Username = username,
         Roles = roles
     };
}
