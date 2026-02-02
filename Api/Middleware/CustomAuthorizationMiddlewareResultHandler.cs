using API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Authorization.Policy;

namespace Api.Middleware;

/// <summary>
/// Custom authorization middleware result handler that provides custom responses
/// for authentication and authorization failures.
/// </summary>
public class CustomAuthorizationMiddlewareResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly IAuthorizationMiddlewareResultHandler _defaultHandler = new AuthorizationMiddlewareResultHandler();
    private readonly ILogger<CustomAuthorizationMiddlewareResultHandler> _logger;

    public CustomAuthorizationMiddlewareResultHandler(
   ILogger<CustomAuthorizationMiddlewareResultHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(
          RequestDelegate next,
          HttpContext context,
          AuthorizationPolicy policy,
          PolicyAuthorizationResult authorizeResult)
    {
        // If authorization succeeded, continue with the request
        if (authorizeResult.Succeeded)
        {
            await next(context);
            return;
        }

        // If user is not authenticated (no token or invalid token)
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";

            var response = new ApiResponse<object>
            {
                Success = false,
                Code = 401,
                SubCode = 0,
                UserMessage = "Authentication required. Please login to access this resource.",
                SystemMessage = "Unauthorized: Missing or invalid authentication token.",
                Data = new
                {
                    ErrorCode = "AUTHENTICATION_REQUIRED",
                    Reason = "No valid authentication token provided"
                },
                ServerTime = DateTimeOffset.UtcNow
            };

            _logger.LogWarning("Unauthorized access attempt to {Path} from {IP}", context.Request.Path, context.Connection.RemoteIpAddress);

            await context.Response.WriteAsJsonAsync(response);
            return;
        }

        // If user is authenticated but lacks required permissions (403 Forbidden)
        if (authorizeResult.Forbidden)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";

            // Extract required permission from policy
            var requiredPermission = GetRequiredPermission(policy);
            var userId = context.User.FindFirst("user_id")?.Value;
            var username = context.User.Identity?.Name;

            var response = new ApiResponse<object>
            {
                Success = false,
                Code = 403,
                SubCode = 0,
                UserMessage = "You do not have permission to access this resource.",
                SystemMessage = $"Forbidden: User lacks required permission '{requiredPermission}'.",
                Data = new
                {
                    ErrorCode = "INSUFFICIENT_PERMISSIONS",
                    RequiredPermission = requiredPermission,
                    Reason = "Your account does not have the necessary permissions for this action"
                },
                ServerTime = DateTimeOffset.UtcNow
            };

            _logger.LogWarning(
                     "Permission denied for user {UserId} ({Username}) attempting to access {Path}. Required permission: {Permission}",
                     userId,
                     username,context.Request.Path,
                     requiredPermission);

            await context.Response.WriteAsJsonAsync(response);
            return;
        }

        // For other authorization failures, use the default handler
        await _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
    }

    /// <summary>
    /// Extracts the required permission from the authorization policy.
    /// </summary>
    private string? GetRequiredPermission(AuthorizationPolicy policy)
    {
        // Look for ClaimsAuthorizationRequirement with "permission" claim
        var claimRequirement = policy.Requirements
            .OfType<ClaimsAuthorizationRequirement>()
            .FirstOrDefault(r => r.ClaimType == "permission");

        if (claimRequirement != null && claimRequirement.AllowedValues != null)
        {
            return string.Join(", ", claimRequirement.AllowedValues);
        }

        // Fallback: try to get policy name
        return policy.ToString();
    }
}
