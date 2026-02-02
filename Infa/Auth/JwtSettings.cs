namespace Infa.Auth;

/// <summary>
/// Configuration options for JWT authentication.
/// </summary>
public class JwtSettings
{
    public const string SectionName = "Jwt";

    /// <summary>
    /// The secret key used to sign JWT tokens.
    /// Must be at least 32 characters for HS256.
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// The issuer claim for generated tokens.
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// The audience claim for generated tokens.
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Access token lifetime in minutes.
    /// Default: 15 minutes (recommended for security).
    /// </summary>
    public int AccessTokenExpirationMinutes { get; set; } = 15;

    /// <summary>
/// Refresh token lifetime in days.
    /// Default: 7 days.
    /// </summary>
    public int RefreshTokenExpirationDays { get; set; } = 7;
}
