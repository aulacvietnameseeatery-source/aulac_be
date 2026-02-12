using Microsoft.AspNetCore.Http;

namespace Api.Helpers;

/// <summary>
/// Helper class for managing refresh token cookies with consistent security settings.
/// </summary>
public static class RefreshTokenCookieHelper
{
    /// <summary>
    /// Cookie name for the refresh token.
    /// </summary>
    public const string CookieName = "refresh_token";

    /// <summary>
    /// Cookie path - restricted to the refresh endpoint for security.
    /// </summary>
    public const string CookiePath = "/api/auth/refresh";

    /// <summary>
    /// Creates standardized cookie options for the refresh token.
    /// </summary>
    /// <param name="expiresAt">When the cookie should expire</param>
    /// <param name="isProduction">Whether running in production (affects Secure flag)</param>
    /// <returns>Configured cookie options</returns>
    public static CookieOptions CreateOptions(DateTime expiresAt, bool isProduction)
    {
        return new CookieOptions
        {
            HttpOnly = true,     // Prevents JavaScript access
            Secure = true,     // HTTPS only in production, allow HTTP in dev
            SameSite = SameSiteMode.None, // CSRF protection
            Path = CookiePath,            // Restrict to refresh endpoint only
            Expires = expiresAt,     // Align with refresh token lifetime
            IsEssential = true       // Not subject to consent policies
        };
    }

    /// <summary>
    /// Creates cookie options for deleting the refresh token cookie.
    /// </summary>
    /// <param name="isProduction">Whether running in production</param>
    /// <returns>Cookie options configured for deletion</returns>
    public static CookieOptions CreateDeleteOptions(bool isProduction)
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Path = CookiePath,
            Expires = DateTime.UtcNow.AddDays(-1), // Expire in the past
            IsEssential = true
        };
    }

    /// <summary>
    /// Sets the refresh token cookie in the HTTP response.
    /// </summary>
    /// <param name="response">The HTTP response</param>
    /// <param name="refreshToken">The refresh token value</param>
    /// <param name="expiresAt">When the token expires</param>
    /// <param name="isProduction">Whether running in production</param>
    public static void SetCookie(HttpResponse response, string refreshToken, DateTime expiresAt, bool isProduction)
    {
        response.Cookies.Append(CookieName, refreshToken, CreateOptions(expiresAt, isProduction));
    }

    /// <summary>
    /// Deletes the refresh token cookie from the HTTP response.
    /// </summary>
    /// <param name="response">The HTTP response</param>
    /// <param name="isProduction">Whether running in production</param>
    public static void DeleteCookie(HttpResponse response, bool isProduction)
    {
        response.Cookies.Delete(CookieName, CreateDeleteOptions(isProduction));
    }

    /// <summary>
    /// Gets the refresh token from the HTTP request cookies.
    /// </summary>
    /// <param name="request">The HTTP request</param>
    /// <returns>The refresh token if present; otherwise null</returns>
    public static string? GetCookie(HttpRequest request)
    {
        return request.Cookies[CookieName];
    }
}
