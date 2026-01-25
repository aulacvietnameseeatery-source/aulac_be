using Core.DTO.Auth;
using API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Core.Interface.Service.Auth;

namespace Api.Controllers;

/// <summary>
/// Authentication controller providing login, refresh, and logout endpoints.
/// This is a base controller - extend it for business-specific authentication logic.
/// </summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService,
      ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Authenticates a user and returns access and refresh tokens.
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authentication tokens on success</returns>
    /// <response code="200">Authentication successful</response>
    /// <response code="401">Invalid credentials or account locked</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AuthErrorDto>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequestDto request,
        CancellationToken cancellationToken)
    {
        // Get client information for session tracking
        var deviceInfo = Request.Headers.UserAgent.ToString();
        var ipAddress = GetClientIpAddress();

        var loginRequest = new LoginRequest(request.Username, request.Password);
        var result = await _authService.LoginAsync(
              loginRequest, deviceInfo, ipAddress, cancellationToken);

        if (!result.Success)
        {
            _logger.LogWarning(
                "Login failed for user {Username}: {ErrorCode}",
                request.Username,
                result.ErrorCode);

            return Unauthorized(new ApiResponse<AuthErrorDto>
            {
                Success = false,
                Code = 401,
                UserMessage = result.ErrorMessage,
                Data = new AuthErrorDto
                {
                    ErrorCode = result.ErrorCode ?? "AUTH_ERROR",
                    ErrorMessage = result.ErrorMessage ?? "Authentication failed."
                },
                ServerTime = DateTimeOffset.UtcNow
            });
        }

        _logger.LogInformation(
            "User {Username} logged in successfully. SessionId: {SessionId}",
            result.Username,
            result.SessionId);

        return Ok(new ApiResponse<AuthResponseDto>
        {
            Success = true,
            Code = 200,
            UserMessage = "Login successful.",
            Data = new AuthResponseDto
            {
                AccessToken = result.AccessToken!,
                RefreshToken = result.RefreshToken!,
                ExpiresIn = result.ExpiresIn,
                TokenType = "Bearer",
                UserId = result.UserId!.Value,
                Username = result.Username!,
                Roles = result.Roles ?? []
            },
            ServerTime = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Refreshes an access token using a valid refresh token.
    /// Implements token rotation for security.
    /// </summary>
    /// <param name="request">Refresh token request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>New access and refresh tokens</returns>
    /// <response code="200">Token refresh successful</response>
    /// <response code="401">Invalid or expired refresh token</response>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AuthErrorDto>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto request,CancellationToken cancellationToken)
    {
        var refreshRequest = new RefreshTokenRequest(
         request.AccessToken,
            request.RefreshToken);

        var result = await _authService.RefreshTokenAsync(refreshRequest, cancellationToken);

        if (!result.Success)
        {
            _logger.LogWarning("Token refresh failed: {ErrorCode}", result.ErrorCode);

            return Unauthorized(new ApiResponse<AuthErrorDto>
            {
                Success = false,
                Code = 401,
                UserMessage = result.ErrorMessage,
                Data = new AuthErrorDto
                {
                    ErrorCode = result.ErrorCode ?? "REFRESH_ERROR",
                    ErrorMessage = result.ErrorMessage ?? "Token refresh failed."
                },
                ServerTime = DateTimeOffset.UtcNow
            });
        }

        _logger.LogInformation(
                 "Token refreshed for user {Username}. SessionId: {SessionId}",
                 result.Username,
                 result.SessionId);

        return Ok(new ApiResponse<AuthResponseDto>
        {
            Success = true,
            Code = 200,
            UserMessage = "Token refresh successful.",
            Data = new AuthResponseDto
            {
                AccessToken = result.AccessToken!,
                RefreshToken = result.RefreshToken!,
                ExpiresIn = result.ExpiresIn,
                TokenType = "Bearer",
                UserId = result.UserId!.Value,
                Username = result.Username!,
                Roles = result.Roles ?? []
            },
            ServerTime = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Logs out the current session (revokes the session).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Logout confirmation</returns>
    /// <response code="200">Logout successful</response>
    /// <response code="401">Not authenticated</response>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var sessionId = GetCurrentSessionId();
        if (!sessionId.HasValue)
        {
            return Unauthorized();
        }

        var success = await _authService.LogoutAsync(sessionId.Value, cancellationToken);

        _logger.LogInformation("User logged out. SessionId: {SessionId}", sessionId);

        return Ok(new ApiResponse<object>
        {
            Success = success,
            Code = 200,
            UserMessage = "Logout successful.",
            Data = new { },
            ServerTime = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Logs out all sessions for the current user except the current one.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of sessions revoked</returns>
    /// <response code="200">Logout all successful</response>
    /// <response code="401">Not authenticated</response>
    [HttpPost("logout-all")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LogoutAll(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var sessionId = GetCurrentSessionId();

        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        var revokedCount = await _authService.LogoutAllAsync(
    userId.Value,
            sessionId,
            cancellationToken);

        _logger.LogInformation(
            "User {UserId} logged out from all devices. Sessions revoked: {Count}",
            userId,
            revokedCount);

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Code = 200,
            UserMessage = $"Logged out from {revokedCount} other device(s).",
            Data = new { SessionsRevoked = revokedCount },
            ServerTime = DateTimeOffset.UtcNow
        });
    }

    #region Helper Methods

    /// <summary>
    /// Gets the current user's ID from JWT claims.
    /// </summary>
    private long? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("user_id");
        if (userIdClaim != null && long.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }
        return null;
    }

    /// <summary>
    /// Gets the current session ID from JWT claims.
    /// </summary>
    private long? GetCurrentSessionId()
    {
        var sessionIdClaim = User.FindFirst("session_id");
        if (sessionIdClaim != null && long.TryParse(sessionIdClaim.Value, out var sessionId))
        {
            return sessionId;
        }
        return null;
    }

    /// <summary>
    /// Gets the client's IP address, considering proxies.
    /// </summary>
    private string? GetClientIpAddress()
    {
        // Check for forwarded IP (behind reverse proxy)
        var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',').First().Trim();
        }

        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }

    #endregion
}
