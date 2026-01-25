using Core.Interface.Repo;
using Core.Interface.Service.Auth;
using Core.Service;
using Infa.Repo;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Infa.Auth;

/// <summary>
/// Extension methods for configuring authentication services.
/// </summary>
public static class AuthServiceExtensions
{
    /// <summary>
    /// Adds authentication infrastructure services to the DI container.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddAuthInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Bind JWT settings from configuration
        services.Configure<JwtSettings>(
             configuration.GetSection(JwtSettings.SectionName));

        // Register auth services
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IAuthSessionRepository, AuthSessionRepository>();
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }

    /// <summary>
    /// Configures JWT Bearer authentication with session validation.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration
        .GetSection(JwtSettings.SectionName)
        .Get<JwtSettings>()
        ?? throw new InvalidOperationException("JWT settings not configured.");

        var signingKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSettings.SecretKey));

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
      {
          options.TokenValidationParameters = new TokenValidationParameters
          {
              ValidateIssuer = true,
              ValidateAudience = true,
              ValidateLifetime = true,
              ValidateIssuerSigningKey = true,
              ValidIssuer = jwtSettings.Issuer,
              ValidAudience = jwtSettings.Audience,
              IssuerSigningKey = signingKey,
              // Reduce clock skew for tighter security
              ClockSkew = TimeSpan.FromSeconds(30)
          };

          // Configure JWT Bearer events for session validation
          options.Events = new JwtBearerEvents
          {
              OnTokenValidated = async context =>
            {
                // Extract session ID from token claims
                var sessionIdClaim = context.Principal?.FindFirst("session_id");
                if (sessionIdClaim == null || !long.TryParse(sessionIdClaim.Value, out var sessionId))
                {
                    context.Fail("Session information not found in token.");
                    return;
                }

                // Validate session against database
                var authService = context.HttpContext.RequestServices.GetRequiredService<IAuthService>();

                var isValid = await authService.ValidateSessionAsync(sessionId);
                if (!isValid)
                {
                    context.Fail("Session has been revoked or expired.");
                    return;
                }

                // Session is valid - request continues
            },

              OnAuthenticationFailed = context =>
              {
                  // Log authentication failures (optional)
                  if (context.Exception is SecurityTokenExpiredException)
                  {
                      context.Response.Headers["Token-Expired"] = "true";
                  }
                  return Task.CompletedTask;
              }
          };
      });

        return services;
    }
}
