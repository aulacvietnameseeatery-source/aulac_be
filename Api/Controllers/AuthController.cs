using Core.DTO.Auth;
using API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Core.Interface.Service.Auth;
using Api.Helpers;

namespace Api.Controllers;

/// <summary>
/// Authentication controller providing login, refresh, logout, and password reset endpoints.
/// </summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;
    private readonly ITokenService _tokenService;
    private readonly bool _isProduction;

    public AuthController(
        IAuthService authService,
        ILogger<AuthController> logger,
        ITokenService tokenService,
        IWebHostEnvironment environment)
    {
        _authService = authService;
        _logger = logger;
        _tokenService = tokenService;
        _isProduction = environment.IsProduction();
    }

    /// <summary>
    /// Authenticates a user and returns access and refresh tokens.
    /// </summary>
    /// <param name="request">Login credentials (username or email + password)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authentication tokens on success</returns>
    /// <response code="200">Authentication successful</response>
    /// <response code="401">Invalid credentials or account locked</response>
    /// <remarks>
    /// **Login Options:** 
    /// - You can login using either your **username** or **email address**
    /// - The system will automatically detect which one you're using
    /// 
    /// **Security:** The refresh token is set as a secure HttpOnly cookie and is NOT included in the JSON response.
    /// This prevents XSS attacks from stealing the refresh token.
    /// 
    /// **Cookie Details:**
    /// - Name: `refresh_token`
    /// - HttpOnly: true (JavaScript cannot access)
    /// - Secure: true in production (HTTPS only)
    /// - SameSite: Strict (CSRF protection)
    /// - Path: `/api/auth/refresh` (restricted scope)
    /// - Expires: Aligned with refresh token lifetime
    /// 
    /// </remarks>
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

        // Set refresh token in HttpOnly cookie (SECURITY: not accessible by JavaScript)
        var refreshTokenExpiry = DateTime.UtcNow.Add(_tokenService.RefreshTokenLifetime);
        RefreshTokenCookieHelper.SetCookie(Response, result.RefreshToken!, refreshTokenExpiry, _isProduction);

        // Return response WITHOUT refresh token in JSON
        return Ok(new ApiResponse<AuthResponseDto>
        {
            Success = true,
            Code = 200,
            UserMessage = "Login successful.",
            Data = new AuthResponseDto
            {
                AccessToken = result.AccessToken!,
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
    /// <remarks>
    /// **Security:** The refresh token is read from the `refresh_token` HttpOnly cookie.
    /// A new refresh token is generated and rotated into the cookie.
    /// 
    /// **Client Requirements:**
    /// - Must send requests with credentials enabled (`credentials: 'include'` in fetch/axios)
    /// - Cookie must be present and valid
    /// 
    /// **Token Rotation:**
    /// - Each refresh generates a new refresh token
    /// - Old refresh token is invalidated
    /// - Prevents token reuse attacks
    /// </remarks>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AuthErrorDto>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto request, CancellationToken cancellationToken)
    {
        // Read refresh token from HttpOnly cookie
        var refreshToken = RefreshTokenCookieHelper.GetCookie(Request);

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            _logger.LogWarning("Token refresh failed: No refresh token cookie found");

            return Unauthorized(new ApiResponse<AuthErrorDto>
            {
                Success = false,
                Code = 401,
                UserMessage = "Refresh token not found. Please log in again.",
                Data = new AuthErrorDto
                {
                    ErrorCode = "REFRESH_TOKEN_MISSING",
                    ErrorMessage = "Refresh token cookie is missing or empty."
                },
                ServerTime = DateTimeOffset.UtcNow
            });
        }

        var refreshRequest = new RefreshTokenRequest(
            request.AccessToken,
            refreshToken);

        var result = await _authService.RefreshTokenAsync(refreshRequest, cancellationToken);

        if (!result.Success)
        {
            _logger.LogWarning("Token refresh failed: {ErrorCode}", result.ErrorCode);

            // Delete invalid cookie
            RefreshTokenCookieHelper.DeleteCookie(Response, _isProduction);

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

        // Rotate refresh token in cookie
        var refreshTokenExpiry = DateTime.UtcNow.Add(_tokenService.RefreshTokenLifetime);
        RefreshTokenCookieHelper.SetCookie(Response, result.RefreshToken!, refreshTokenExpiry, _isProduction);

        // Return response WITHOUT refresh token in JSON
        return Ok(new ApiResponse<AuthResponseDto>
        {
            Success = true,
            Code = 200,
            UserMessage = "Token refresh successful.",
            Data = new AuthResponseDto
            {
                AccessToken = result.AccessToken!,
                // RefreshToken is NOT set here - it's rotated in the cookie
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
    /// <remarks>
    /// **Security:** Deletes the refresh token cookie and revokes the session in the database.
    /// </remarks>
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

        // Delete refresh token cookie
        RefreshTokenCookieHelper.DeleteCookie(Response, _isProduction);

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
    /// <remarks>
    /// **Note:** This endpoint does NOT delete the current session's refresh token cookie,
    /// as the current session remains active.
    /// </remarks>
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

        // Note: We keep the current session's cookie since this session remains active

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

    /// <summary>
    /// Initiates a password reset request by sending a reset link to the user's email.
    /// </summary>
    /// <param name="request">Request containing the email address</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generic success message (does not reveal if email exists)</returns>
    /// <response code="200">Request processed successfully</response>
    /// <remarks>
    /// **Security Considerations:**
    /// 
    /// - **User Enumeration Prevention**: This endpoint ALWAYS returns HTTP 200 with a success message,
    ///   regardless of whether the email exists in the system. This prevents attackers from using this
    ///   endpoint to discover valid email addresses.
    /// 
    /// - **Timing Attack Mitigation**: The service layer includes random delays for non-existent emails
    ///   to ensure response times are consistent, preventing timing-based enumeration.
    /// 
    /// - **Token Security**: Generated tokens are cryptographically random (256-bit), hashed before storage,
    ///   and expire after 30 minutes.
    /// 
    /// - **Email Delivery**: Reset emails are queued asynchronously and processed by a background worker,
    ///   ensuring the API response is not delayed by email delivery.
    /// </remarks>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request, CancellationToken cancellationToken)
    {
        // Capture client metadata for security audit logging
        var ipAddress = GetClientIpAddress();
        var userAgent = Request.Headers.UserAgent.ToString();

        try
        {
            // Call service - this will handle the entire flow including email queueing
            await _authService.RequestPasswordResetAsync(
                request.Email,
                ipAddress,
                userAgent,
                cancellationToken);

            // Log the request (without revealing whether email exists)
            _logger.LogInformation(
                "Password reset requested for email pattern {EmailPattern} from IP {IP}",
                MaskEmail(request.Email),
                ipAddress);
        }
        catch (Exception ex)
        {
            // Log the error but still return success to prevent enumeration
            _logger.LogError(ex,
               "Error processing password reset request from IP {IP}",
                ipAddress);
        }

        // SECURITY: Always return success, even if email doesn't exist or an error occurred
        // This prevents attackers from determining which emails are registered in the system
        return Ok(new ApiResponse<object>
        {
            Success = true,
            Code = 200,
            UserMessage = "If an account with that email exists, a password reset link has been sent. Please check your email.",
            Data = new { },
            ServerTime = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Verifies if a password reset token is valid and not expired.
    /// </summary>
    /// <param name="request">Request containing the reset token</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result indicating if the token is valid</returns>
    /// <response code="200">Token verification completed</response>
    /// <remarks>
    /// **Purpose:**
    /// 
    /// This endpoint allows the frontend to verify a token before showing the password reset form.
    /// 
    /// **Security Notes:**
    /// - Does not consume the token - verification can be called multiple times
    /// - Does not reveal user information associated with the token
    /// - Should be called when user lands on the reset password page
    /// - Returns simple boolean indicating validity
    /// </remarks>
    [HttpPost("reset-password/verify")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<VerifyResetTokenResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> VerifyResetToken(
        [FromBody] VerifyResetTokenRequestDto request,
      CancellationToken cancellationToken)
    {
        var isValid = await _authService.VerifyPasswordResetTokenAsync(request.Token, cancellationToken);

        // Log verification attempt (without logging the token itself)
        _logger.LogInformation(
            "Password reset token verification: {IsValid} from IP {IP}",
            isValid,
            GetClientIpAddress());

        return Ok(new ApiResponse<VerifyResetTokenResponseDto>
        {
            Success = true,
            Code = 200,
            UserMessage = isValid
         ? "Token is valid."
             : "Token is invalid or has expired.",
            Data = new VerifyResetTokenResponseDto
            {
                Valid = isValid
            },
            ServerTime = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Resets a user's password using a valid reset token.
    /// </summary>
    /// <param name="request">Request containing the reset token and new password</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success confirmation</returns>
    /// <response code="200">Password reset successful</response>
    /// <response code="400">Invalid or expired token, or password validation failed</response>
    /// <remarks>
    /// **Security Flow:**
    /// 
    /// 1. **Token Validation**: Verifies the token is valid and not expired
    /// 2. **User Lookup**: Retrieves the user associated with the token
    /// 3. **Password Hashing**: Hashes the new password using BCrypt (or configured hasher)
    /// 4. **Database Update**: Updates the password hash in the database
    /// 5. **Token Consumption**: Deletes the token (one-time use only)
    /// 6. **Session Revocation**: Invalidates ALL active sessions for security, forcing re-login everywhere
    /// 
    /// **Post-Reset Behavior:**
    /// 
    /// After a successful password reset:
    /// - The reset token is permanently deleted
    /// - All active sessions are revoked (user must re-login on all devices)
    /// - Any existing password reset tokens for the user are also invalidated
    /// 
    /// **Password Requirements:**
    /// 
    /// - Minimum 8 characters (enforced by DTO validation)
    /// - Maximum 128 characters
    /// - Consider adding complexity requirements in production (uppercase, lowercase, digit, special char)
    /// </remarks>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            // Execute the password reset
            await _authService.ResetPasswordAsync(
                 request.Token,
             request.NewPassword,
               cancellationToken);

            _logger.LogInformation(
                "Password reset completed successfully from IP {IP}",
                GetClientIpAddress());

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Code = 200,
                UserMessage = "Your password has been reset successfully. You can now log in with your new password.",
                Data = new { },
                ServerTime = DateTimeOffset.UtcNow
            });
        }
        catch (InvalidOperationException ex)
        {
            // Token is invalid, expired, or user account issues
            _logger.LogWarning(
                "Password reset failed: {Message} from IP {IP}",
                ex.Message,
                GetClientIpAddress());

            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Code = 400,
                UserMessage = ex.Message,
                Data = new { },
                ServerTime = DateTimeOffset.UtcNow
            });
        }
        catch (ArgumentException ex)
        {
            // Password validation failed
            _logger.LogWarning(
               "Password reset failed due to validation: {Message} from IP {IP}",
               ex.Message,
               GetClientIpAddress());

            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Code = 400,
                UserMessage = ex.Message,
                Data = new { },
                ServerTime = DateTimeOffset.UtcNow
            });
        }
        catch (Exception ex)
        {
            // Unexpected error
            _logger.LogError(ex,
                "Unexpected error during password reset from IP {IP}",
                GetClientIpAddress());

            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Code = 400,
                UserMessage = "An error occurred while resetting your password. Please try again or request a new reset link.",
                Data = new { },
                ServerTime = DateTimeOffset.UtcNow
            });
        }
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
    /// Gets the client's IP address, considering reverse proxies and load balancers.
    /// </summary>
    /// <returns>Client IP address or null if unavailable</returns>
    private string? GetClientIpAddress()
    {
        // Check for forwarded IP (behind reverse proxy/load balancer)
        var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            // X-Forwarded-For can contain multiple IPs (client, proxy1, proxy2, ...)
            // The first one is the original client
            return forwardedFor.Split(',').First().Trim();
        }

        // Fallback to direct connection IP
        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }

    /// <summary>
    /// Partially masks an email address for logging purposes.
    /// Example: john.doe@example.com -> j***@example.com
    /// </summary>
    /// <param name="email">The email to mask</param>
    /// <returns>Masked email for safe logging</returns>
    private static string MaskEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            return "***";

        var parts = email.Split('@');
        if (parts[0].Length <= 1)
            return $"*@{parts[1]}";

        return $"{parts[0][0]}***@{parts[1]}";
    }

    #endregion
}
