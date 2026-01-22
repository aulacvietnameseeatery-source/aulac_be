using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace Core.Interface.Service;

/// <summary>
/// Token service abstraction for generating and managing authentication tokens.
/// This interface defines the contract for token operations without any infrastructure dependencies.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generates an access token (JWT) for the authenticated user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user</param>
    /// <param name="username">The username of the user</param>
    /// <param name="sessionId">The session identifier to embed in the token</param>
    /// <param name="roles">Collection of role names assigned to the user</param>
    /// <param name="permissions">Collection of permission codes assigned to the user</param>
    /// <returns>The generated access token string</returns>
    string GenerateAccessToken(
        long userId,
        string username,
        long sessionId,
        IEnumerable<string> roles,
        IEnumerable<string>? permissions = null);

    /// <summary>
    /// Generates a cryptographically secure refresh token.
    /// </summary>
    /// <returns>A random, URL-safe refresh token string</returns>
    string GenerateRefreshToken();

/// <summary>
    /// Hashes a refresh token for secure storage.
    /// Raw tokens should never be stored in the database.
    /// </summary>
    /// <param name="token">The raw refresh token to hash</param>
    /// <returns>The hashed token value</returns>
    string HashToken(string token);

    /// <summary>
    /// Verifies if a raw token matches a stored hash.
    /// </summary>
    /// <param name="rawToken">The raw token to verify</param>
    /// <param name="hashedToken">The stored hash to compare against</param>
    /// <returns>True if the token matches the hash; otherwise false</returns>
    bool VerifyToken(string rawToken, string hashedToken);

    /// <summary>
    /// Gets the configured access token lifetime.
    /// </summary>
    TimeSpan AccessTokenLifetime { get; }

    /// <summary>
    /// Gets the configured refresh token lifetime.
    /// </summary>
    TimeSpan RefreshTokenLifetime { get; }

    /// <summary>
    /// Extracts claims from an access token without validating its signature.
    /// Used for extracting session information from expired tokens during refresh.
    /// </summary>
    /// <param name="token">The access token to parse</param>
    /// <returns>The claims principal if parsing succeeds; otherwise null</returns>
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
