using System.ComponentModel.DataAnnotations;

namespace Core.DTO.Auth;

/// <summary>
/// Login request model.
/// </summary>
public class LoginRequestDto
{
    /// <summary>
    /// The username for authentication.
    /// </summary>
    [Required(ErrorMessage = "Username is required.")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Username must be between 1 and 100 characters.")]
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

    /// <summary>
    /// The refresh token issued during login or previous refresh.
    /// </summary>
    [Required(ErrorMessage = "Refresh token is required.")]
    public string RefreshToken { get; set; } = string.Empty;
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
    /// The refresh token for obtaining new access tokens.
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

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
