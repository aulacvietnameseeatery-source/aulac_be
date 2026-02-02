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
