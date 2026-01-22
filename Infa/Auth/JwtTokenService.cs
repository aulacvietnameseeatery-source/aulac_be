using Core.Interface.Service;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Infa.Auth;

/// <summary>
/// JWT-based implementation of ITokenService.
/// Handles access token generation, refresh token generation, and token hashing.
/// </summary>
public class JwtTokenService : ITokenService
{
    private readonly JwtSettings _jwtSettings;
    private readonly SymmetricSecurityKey _signingKey;

    public JwtTokenService(IOptions<JwtSettings> jwtSettings)
    {
        _jwtSettings = jwtSettings.Value;
        _signingKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
    }

    /// <inheritdoc />
    public TimeSpan AccessTokenLifetime =>
        TimeSpan.FromMinutes(_jwtSettings.AccessTokenExpirationMinutes);

    /// <inheritdoc />
    public TimeSpan RefreshTokenLifetime =>
        TimeSpan.FromDays(_jwtSettings.RefreshTokenExpirationDays);

    /// <inheritdoc />
    public string GenerateAccessToken(
        long userId,
        string username,
        long sessionId,
        IEnumerable<string> roles,
        IEnumerable<string>? permissions = null)
    {
        var claims = new List<Claim>
        {
            // Standard claims
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, username),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),

            // Custom claims for session validation
            new("session_id", sessionId.ToString()),
            new("user_id", userId.ToString()),
     };

        // Add role claims
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        // Add permission claims (optional)
        if (permissions != null)
        {
            foreach (var permission in permissions)
            {
                claims.Add(new Claim("permission", permission));
            }
        }

        var credentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.Add(AccessTokenLifetime),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <inheritdoc />
    public string GenerateRefreshToken()
    {
        // Generate a cryptographically secure random token
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);

        // Convert to URL-safe Base64
        return Convert.ToBase64String(randomBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }

    /// <inheritdoc />
    public string HashToken(string token)
    {
        // Use SHA256 for hashing refresh tokens
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }

    /// <inheritdoc />
    public bool VerifyToken(string rawToken, string hashedToken)
    {
        var computedHash = HashToken(rawToken);
        return computedHash == hashedToken;
    }

    /// <inheritdoc />
    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidateIssuer = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _jwtSettings.Issuer,
            ValidAudience = _jwtSettings.Audience,
            IssuerSigningKey = _signingKey,
            // Allow expired tokens for refresh flow
            ValidateLifetime = false
        };

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(
                 token,
                 tokenValidationParameters,
            out var securityToken);

            // Verify it's a valid JWT with correct algorithm
            if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                      !jwtSecurityToken.Header.Alg.Equals(
             SecurityAlgorithms.HmacSha256,
                    StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }

            return principal;
        }
        catch
        {
            return null;
        }
    }
}
