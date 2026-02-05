using Core.DTO.Account;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Core.DTO.Auth;

/// <summary>
/// Login request model.
/// </summary>
public class LoginRequestDto
{
    /// <summary>
    /// The username or email address for authentication.
    /// Can be either the user's username or their registered email address.
    /// </summary>
    [Required(ErrorMessage = "Username or email is required.")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Username or email must be between 1 and 100 characters.")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// The password for authentication.
    /// </summary>
    [Required(ErrorMessage = "Password is required.")]
    [StringLength(255, MinimumLength = 1, ErrorMessage = "Password must be between 1 and 255 characters.")]
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Refresh token request model.
/// </summary>
public class RefreshTokenRequestDto
{
    /// <summary>
    /// The current (possibly expired) access token.
    /// </summary>
    [Required(ErrorMessage = "Access token is required.")]
    public string AccessToken { get; set; } = string.Empty;
}

/// <summary>
/// Authentication response model.
/// </summary>
public class AuthResponseDto
{
    /// <summary>
    /// The JWT access token for API authentication.
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Access token lifetime in seconds.
    /// </summary>
    public int ExpiresIn { get; set; }

    /// <summary>
    /// Token type (always "Bearer").
    /// </summary>
    public string TokenType { get; set; } = "Bearer";

    /// <summary>
    /// The authenticated user's ID.
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// The authenticated user's username.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// The roles assigned to the user.
    /// </summary>
    public IEnumerable<string> Roles { get; set; } = [];
}

/// <summary>
/// Error response model.
/// </summary>
public class AuthErrorDto
{
    /// <summary>
    /// Error code for programmatic handling.
    /// </summary>
    public string ErrorCode { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable error message.
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;
}

/// <summary>
/// Request model for initiating a password reset.
/// </summary>
public class ForgotPasswordRequestDto
{
    /// <summary>
    /// The email address associated with the account.
    /// </summary>
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    [StringLength(255, ErrorMessage = "Email cannot exceed 255 characters.")]
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Request model for verifying a password reset token.
/// </summary>
public class VerifyResetTokenRequestDto
{
    /// <summary>
    /// The password reset token to verify.
    /// </summary>
    [Required(ErrorMessage = "Token is required.")]
    public string Token { get; set; } = string.Empty;
}

/// <summary>
/// Response model for token verification.
/// </summary>
public class VerifyResetTokenResponseDto
{
    /// <summary>
    /// Indicates whether the token is valid and not expired.
    /// </summary>
    public bool Valid { get; set; }
}

/// <summary>
/// Request model for resetting a password.
/// </summary>
public class ResetPasswordRequestDto
{
    /// <summary>
    /// The password reset token received via email.
    /// </summary>
    [Required(ErrorMessage = "Token is required.")]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// The new password to set for the account.
    /// </summary>
    [Required(ErrorMessage = "New password is required.")]
    [StringLength(128, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 128 characters.")]
    public string NewPassword { get; set; } = string.Empty;
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
/// Account data transfer object (legacy).
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

    /// <summary>
    /// Indicates if the user must change their password before continuing.
    /// </summary>
    public bool RequirePasswordChange { get; init; }

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
         Roles = roles,
         RequirePasswordChange = false
     };

    public static AuthResult PasswordChangeRequired(
          string tempAccessToken,
       int expiresIn,
        long sessionId,
          long userId,
          string username,
          string message) =>
          new()
          {
              Success = true,
              AccessToken = tempAccessToken,
              RefreshToken = null, // No refresh token for password change session
              ExpiresIn = expiresIn,
              SessionId = sessionId,
              UserId = userId,
              Username = username,
              RequirePasswordChange = true,
              ErrorCode = "PASSWORD_CHANGE_REQUIRED",
              ErrorMessage = message
          };
}