using Core.Attribute;
using Core.Data;
using Core.DTO.SystemSetting;
using Core.Interface.Service.Entity;
using Core.Interface.Service.FileStorage;
using Core.DTO.General;
using Microsoft.AspNetCore.Mvc;
using API.Models;
using Microsoft.AspNetCore.Authorization;

namespace Api.Controllers;

/// <summary>
/// Controller for managing system settings.
/// Provides endpoints for reading and updating application configuration.
/// </summary>
[Route("api/system-settings")]
[ApiController]
public class SystemSettingController : ControllerBase
{
    private readonly ISystemSettingService _systemSettingService;
    private readonly IFileStorage _fileStorage;
    private readonly ILogger<SystemSettingController> _logger;

    private static readonly HashSet<string> StoreMediaKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "store.intro.hero.image",
        "store.intro.virtualTour.videoUrl",
        "store.intro.virtualTour.videoUrlLeft",
        "store.intro.virtualTour.videoUrlRight",
        "store.intro.collection.dish1.image",
        "store.intro.collection.dish2.image",
        "store.intro.collection.dish3.image",
        "store.logoUrl"
    };

    public SystemSettingController(
        ISystemSettingService systemSettingService,
        IFileStorage fileStorage,
        ILogger<SystemSettingController> logger)
    {
        _systemSettingService = systemSettingService;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves all non-sensitive system settings.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary of all non-sensitive settings</returns>
    /// <response code="200">Settings retrieved successfully</response>
    /// <remarks>
    /// This endpoint returns all system settings that are not marked as sensitive.
    /// Sensitive settings (passwords, API keys, etc.) are excluded for security.
    /// </remarks>
    [HttpGet]
    [HasPermission(Permissions.ViewSystemSettings)]
    [ProducesResponseType(typeof(ApiResponse<Dictionary<string, object?>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllNonSensitive(CancellationToken cancellationToken = default)
    {
        try
        {
            var settings = await _systemSettingService.GetAllNonSensitiveAsync(cancellationToken);

            _logger.LogInformation("Retrieved {Count} non-sensitive system settings", settings.Count);

            return Ok(new ApiResponse<Dictionary<string, object?>>
            {
                Success = true,
                Code = 200,
                UserMessage = "System settings retrieved successfully.",
                Data = settings,
                ServerTime = DateTimeOffset.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving system settings");

            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Code = 500,
                UserMessage = "An error occurred while retrieving system settings.",
                SystemMessage = ex.Message,
                Data = new { },
                ServerTime = DateTimeOffset.UtcNow
            });
        }
    }

    /// <summary>
    /// Retrieves a specific system setting by key.
    /// </summary>
    /// <param name="key">The setting key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The setting value</returns>
    /// <response code="200">Setting found</response>
    /// <response code="404">Setting not found</response>
    [HttpGet("{key}")]
    [HasPermission(Permissions.ViewSystemSettings)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByKey(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var value = await _systemSettingService.GetStringAsync(key, cancellationToken: cancellationToken);

            if (value == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Code = 404,
                    UserMessage = $"System setting '{key}' not found.",
                    Data = new { },
                    ServerTime = DateTimeOffset.UtcNow
                });
            }

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Code = 200,
                UserMessage = "System setting retrieved successfully.",
                Data = new { Key = key, Value = value },
                ServerTime = DateTimeOffset.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving system setting {Key}", key);

            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Code = 500,
                UserMessage = "An error occurred while retrieving the system setting.",
                SystemMessage = ex.Message,
                Data = new { },
                ServerTime = DateTimeOffset.UtcNow
            });
        }
    }


    /// <summary>
    /// Creates a new system setting with a specific type.
    /// </summary>
    /// <param name="request">The creation request (key, value, type, etc.)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success confirmation</returns>
    /// <response code="200">Setting created successfully</response>
    [HttpPost]
    [HasPermission(Permissions.ManageSystemSettings)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateSetting(
          [FromBody] CreateSystemSettingDto request,
          CancellationToken cancellationToken = default)
    {
        try
        {
            var userIdClaim = User.FindFirst("user_id");
            long? userId = userIdClaim != null && long.TryParse(userIdClaim.Value, out var id) ? id : null;

            await _systemSettingService.CreateSettingAsync(
                request.Key,
                request.SettingName,
                request.ValueType,
                request.Value,
                description: request.Description,
                isSensitive: request.IsSensitive,
                updatedBy: userId,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "System setting '{Key}' created by user {UserId}",
                request.Key,
                userId);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Code = 200,
                UserMessage = $"System setting '{request.Key}' created successfully.",
                Data = new { Key = request.Key, Value = request.Value, Type = request.ValueType },
                ServerTime = DateTimeOffset.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating system setting {Key}", request.Key);

            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Code = 500,
                UserMessage = "An error occurred while creating the system setting.",
                SystemMessage = ex.Message,
                Data = new { },
                ServerTime = DateTimeOffset.UtcNow
            });
        }
    }

    /// <summary>
    /// Updates a string system setting.
    /// </summary>
    /// <param name="key">The setting key</param>
    /// <param name="request">The update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success confirmation</returns>
    /// <response code="200">Setting updated successfully</response>
    /// <remarks>
    /// Updates an existing system setting or creates a new one if it doesn't exist.
    /// The user ID is extracted from the JWT token for audit purposes.
    /// </remarks>
    [HttpPut("{key}")]
    [HasPermission(Permissions.ManageSystemSettings)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateStringSetting(
          string key,
          [FromBody] UpdateSettingValueDto request,
          CancellationToken cancellationToken = default)
    {
        try
        {
            // Get current user ID from claims
            var userIdClaim = User.FindFirst("user_id");
            long? userId = userIdClaim != null && long.TryParse(userIdClaim.Value, out var id) ? id : null;

            await _systemSettingService.SetStringAsync(
           key,
               request.Value,
                description: request.Description,
             isSensitive: false,
                  updatedBy: userId,
         cancellationToken: cancellationToken);

            _logger.LogInformation(
      "System setting '{Key}' updated by user {UserId}",
             key,
               userId);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Code = 200,
                UserMessage = $"System setting '{key}' updated successfully.",
                Data = new { Key = key, Value = request.Value },
                ServerTime = DateTimeOffset.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating system setting {Key}", key);

            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Code = 500,
                UserMessage = "An error occurred while updating the system setting.",
                SystemMessage = ex.Message,
                Data = new { },
                ServerTime = DateTimeOffset.UtcNow
            });
        }
    }

    /// <summary>
    /// Updates an integer system setting.
    /// </summary>
    /// <param name="key">The setting key</param>
    /// <param name="value">The integer value</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success confirmation</returns>
    /// <response code="200">Setting updated successfully</response>
    [HttpPut("{key}/int")]
    [HasPermission(Permissions.ManageSystemSettings)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateIntSetting(
   string key,
        [FromBody] long value,
     CancellationToken cancellationToken = default)
    {
        try
        {
            var userIdClaim = User.FindFirst("user_id");
            long? userId = userIdClaim != null && long.TryParse(userIdClaim.Value, out var id) ? id : null;

            await _systemSettingService.SetIntAsync(
       key,
          value,
        updatedBy: userId,
  cancellationToken: cancellationToken);

            _logger.LogInformation("Integer setting '{Key}' updated to {Value}", key, value);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Code = 200,
                UserMessage = $"System setting '{key}' updated successfully.",
                Data = new { Key = key, Value = value },
                ServerTime = DateTimeOffset.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating integer setting {Key}", key);

            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Code = 500,
                UserMessage = "An error occurred while updating the system setting.",
                SystemMessage = ex.Message,
                Data = new { },
                ServerTime = DateTimeOffset.UtcNow
            });
        }
    }

    /// <summary>
    /// Updates a boolean system setting.
    /// </summary>
    /// <param name="key">The setting key</param>
    /// <param name="value">The boolean value</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success confirmation</returns>
    /// <response code="200">Setting updated successfully</response>
    [HttpPut("{key}/bool")]
    [HasPermission(Permissions.ManageSystemSettings)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateBoolSetting(
        string key,
        [FromBody] bool value,
      CancellationToken cancellationToken = default)
    {
        try
        {
            var userIdClaim = User.FindFirst("user_id");
            long? userId = userIdClaim != null && long.TryParse(userIdClaim.Value, out var id) ? id : null;

            await _systemSettingService.SetBoolAsync(
             key,
                    value,
          updatedBy: userId,
          cancellationToken: cancellationToken);

            _logger.LogInformation("Boolean setting '{Key}' updated to {Value}", key, value);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Code = 200,
                UserMessage = $"System setting '{key}' updated successfully.",
                Data = new { Key = key, Value = value },
                ServerTime = DateTimeOffset.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating boolean setting {Key}", key);

            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Code = 500,
                UserMessage = "An error occurred while updating the system setting.",
                SystemMessage = ex.Message,
                Data = new { },
                ServerTime = DateTimeOffset.UtcNow
            });
        }
    }

    /// <summary>
    /// Deletes a system setting.
    /// </summary>
    /// <param name="key">The setting key to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success confirmation</returns>
    /// <response code="200">Setting deleted successfully</response>
    /// <response code="404">Setting not found</response>
    [HttpDelete("{key}")]
    [HasPermission(Permissions.ManageSystemSettings)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSetting(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var deleted = await _systemSettingService.DeleteAsync(key, cancellationToken);

            if (!deleted)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Code = 404,
                    UserMessage = $"System setting '{key}' not found.",
                    Data = new { },
                    ServerTime = DateTimeOffset.UtcNow
                });
            }

            _logger.LogWarning("System setting '{Key}' deleted", key);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Code = 200,
                UserMessage = $"System setting '{key}' deleted successfully.",
                Data = new { Key = key },
                ServerTime = DateTimeOffset.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting system setting {Key}", key);

            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Code = 500,
                UserMessage = "An error occurred while deleting the system setting.",
                SystemMessage = ex.Message,
                Data = new { },
                ServerTime = DateTimeOffset.UtcNow
            });
        }
    }

    /// <summary>
    /// Clears the cache for a specific system setting.
    /// </summary>
    /// <param name="key">The setting key</param>
    /// <returns>Success confirmation</returns>
    /// <response code="200">Cache cleared successfully</response>
    [HttpPost("{key}/clear-cache")]
    [HasPermission(Permissions.ManageSystemSettings)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ClearCache(string key)
    {
        try
        {
            await _systemSettingService.ClearCacheAsync(key);

            _logger.LogInformation("Cache cleared for system setting '{Key}'", key);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Code = 200,
                UserMessage = $"Cache cleared for system setting '{key}'.",
                Data = new { Key = key },
                ServerTime = DateTimeOffset.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cache for system setting {Key}", key);

            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Code = 500,
                UserMessage = "An error occurred while clearing the cache.",
                SystemMessage = ex.Message,
                Data = new { },
                ServerTime = DateTimeOffset.UtcNow
            });
        }
    }

    /// <summary>
    /// Uploads a store logo image.
    /// </summary>
    /// <param name="file">The image file (max 5MB)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Public URL of the uploaded image</returns>
    [HttpPost("upload-logo")]
    [HasPermission(Permissions.ManageSystemSettings)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadLogo(IFormFile file, CancellationToken cancellationToken = default)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Code = 400,
                    UserMessage = "No file uploaded.",
                    Data = new { },
                    ServerTime = DateTimeOffset.UtcNow
                });
            }

            var uploadRequest = new FileUploadRequest
            {
                Stream = file.OpenReadStream(),
                FileName = file.FileName,
                ContentType = file.ContentType
            };

            var result = await _fileStorage.SaveAsync(uploadRequest, "store-logo", FileValidationOptions.ImageUpload, cancellationToken);

            _logger.LogInformation("Store logo uploaded: {PublicUrl}", result.PublicUrl);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Code = 200,
                UserMessage = "Logo uploaded successfully.",
                Data = new { RelativePath = result.RelativePath, PublicUrl = result.PublicUrl },
                ServerTime = DateTimeOffset.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading store logo");

            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Code = 500,
                UserMessage = "An error occurred while uploading the logo.",
                SystemMessage = ex.Message,
                Data = new { },
                ServerTime = DateTimeOffset.UtcNow
            });
        }
    }

    /// <summary>
    /// Uploads a generic file (e.g. video or image) for store settings.
    /// </summary>
    /// <param name="file">The file to upload</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Public URL of the uploaded file</returns>
    [HttpPost("upload-file")]
    [HasPermission(Permissions.ManageSystemSettings)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadFile(IFormFile file, CancellationToken cancellationToken = default)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Code = 400,
                    UserMessage = "No file uploaded.",
                    Data = new { },
                    ServerTime = DateTimeOffset.UtcNow
                });
            }

            var uploadRequest = new FileUploadRequest
            {
                Stream = file.OpenReadStream(),
                FileName = file.FileName,
                ContentType = file.ContentType
            };

            var extension = Path.GetExtension(file.FileName);
            var isVideo = file.ContentType.Equals("video/mp4", StringComparison.OrdinalIgnoreCase)
                          || extension.Equals(".mp4", StringComparison.OrdinalIgnoreCase);

            // Keep store folders and enforce store intro video policy (30s, 50MB, MP4).
            var validation = isVideo ? FileValidationOptions.StoreIntroVideo : FileValidationOptions.ImageUpload;
            var folder = isVideo ? "store-videos" : "store-media";

            var result = await _fileStorage.SaveAsync(uploadRequest, folder, validation, cancellationToken);

            _logger.LogInformation("Store {Type} uploaded to {Folder}: {PublicUrl}", 
                isVideo ? "video" : "image", folder, result.PublicUrl);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Code = 200,
                UserMessage = "File uploaded successfully.",
                Data = new { RelativePath = result.RelativePath, PublicUrl = result.PublicUrl },
                ServerTime = DateTimeOffset.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading store media");

            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Code = 500,
                UserMessage = "An error occurred while uploading the file.",
                SystemMessage = ex.Message,
                Data = new { },
                ServerTime = DateTimeOffset.UtcNow
            });
        }
    }

    // ─── Group-Based Endpoints ──────────────────────────────────────────────

    /// <summary>
    /// Retrieves all system settings grouped by their key prefix.
    /// E.g. settings with keys "restaurant.*" are in group "restaurant".
    /// </summary>
    /// <response code="200">Grouped settings returned successfully</response>
    [HttpGet("grouped")]
    [HasPermission(Permissions.ViewSystemSettings)]
    [ProducesResponseType(typeof(ApiResponse<Dictionary<string, object>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllGrouped(CancellationToken cancellationToken = default)
    {
        try
        {
            var grouped = await _systemSettingService.GetAllGroupedAsync(cancellationToken);

            _logger.LogInformation("Retrieved system settings in {Count} groups", grouped.Count);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Code = 200,
                UserMessage = "Grouped system settings retrieved successfully.",
                Data = grouped,
                ServerTime = DateTimeOffset.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving grouped system settings");
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Code = 500,
                UserMessage = "An error occurred while retrieving grouped system settings.",
                SystemMessage = ex.Message,
                Data = new { },
                ServerTime = DateTimeOffset.UtcNow
            });
        }
    }

    /// <summary>
    /// Retrieves all settings (including sensitive) for a specific group.
    /// </summary>
    /// <param name="group">Group prefix, e.g. "password" or "restaurant"</param>
    /// <response code="200">Settings for the group returned</response>
    [HttpGet("groups/{group}")]
    [HasPermission(Permissions.ViewSystemSettings)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGroup(string group, CancellationToken cancellationToken = default)
    {
        try
        {
            var settings = await _systemSettingService.GetGroupAsync(group, cancellationToken);
            if (group.Equals("store", StringComparison.OrdinalIgnoreCase))
            {
                settings = settings.Select(AttachPublicUrlForStoreMedia).ToList();
            }

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Code = 200,
                UserMessage = $"Settings for group '{group}' retrieved successfully.",
                Data = new { GroupName = group, Settings = settings },
                ServerTime = DateTimeOffset.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving settings for group {Group}", group);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Code = 500,
                UserMessage = "An error occurred while retrieving group settings.",
                SystemMessage = ex.Message,
                Data = new { },
                ServerTime = DateTimeOffset.UtcNow
            });
        }
    }

    /// <summary>
    /// Retrieves only non-sensitive settings for a specific group.
    /// This endpoint is public and does not require authentication.
    /// </summary>
    /// <param name="group">Group prefix, e.g. "store"</param>
    /// <response code="200">Settings for the group returned</response>
    [HttpGet("public/groups/{group}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPublicGroup(string group, CancellationToken cancellationToken = default)
    {
        try
        {
            var settings = await _systemSettingService.GetPublicGroupAsync(group, cancellationToken);
            if (group.Equals("store", StringComparison.OrdinalIgnoreCase))
            {
                settings = settings.Select(AttachPublicUrlForStoreMedia).ToList();
            }

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Code = 200,
                UserMessage = $"Public settings for group '{group}' retrieved successfully.",
                Data = new { GroupName = group, Settings = settings },
                ServerTime = DateTimeOffset.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving public settings for group {Group}", group);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Code = 500,
                UserMessage = "An error occurred while retrieving public group settings.",
                SystemMessage = ex.Message,
                Data = new { },
                ServerTime = DateTimeOffset.UtcNow
            });
        }
    }

    /// <summary>
    /// Bulk-updates all settings in a group.
    /// Existing value types and sensitive flags are preserved.
    /// </summary>
    /// <param name="group">Group prefix, e.g. "password" or "restaurant"</param>
    /// <param name="request">List of key-value pairs to update</param>
    /// <response code="200">Group updated successfully</response>
    [HttpPut("groups/{group}")]
    [HasPermission(Permissions.ManageSystemSettings)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> BulkUpdateGroup(
        string group,
        [FromBody] BulkUpdateGroupDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userIdClaim = User.FindFirst("user_id");
            long? userId = userIdClaim != null && long.TryParse(userIdClaim.Value, out var id) ? id : null;

            if (group.Equals("store", StringComparison.OrdinalIgnoreCase))
            {
                request = NormalizeStoreMediaValues(request);
            }

            await _systemSettingService.BulkUpdateGroupAsync(
                group, request.Items, userId, cancellationToken);

            _logger.LogInformation(
                "Bulk-updated {Count} settings in group '{Group}' by user {UserId}",
                request.Items.Count, group, userId);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Code = 200,
                UserMessage = $"Settings for group '{group}' updated successfully.",
                Data = new { GroupName = group, UpdatedCount = request.Items.Count },
                ServerTime = DateTimeOffset.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk-updating settings for group {Group}", group);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Code = 500,
                UserMessage = "An error occurred while updating group settings.",
                SystemMessage = ex.Message,
                Data = new { },
                ServerTime = DateTimeOffset.UtcNow
            });
        }
    }

    private SystemSettingDetailDto AttachPublicUrlForStoreMedia(SystemSettingDetailDto setting)
    {
        if (!StoreMediaKeys.Contains(setting.SettingKey))
            return setting;

        if (setting.Value is not string value || string.IsNullOrWhiteSpace(value))
            return setting;

        var publicUrl = BuildPublicUrl(value);
        return setting with { PublicUrl = publicUrl };
    }

    private string? BuildPublicUrl(string value)
    {
        var trimmed = value.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            return null;

        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var absolute))
            return absolute.ToString();

        if (trimmed.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
        {
            var withoutPrefix = trimmed.Substring("/uploads/".Length);
            return _fileStorage.GetPublicUrl(withoutPrefix);
        }

        if (trimmed.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase))
        {
            var withoutPrefix = trimmed.Substring("uploads/".Length);
            return _fileStorage.GetPublicUrl(withoutPrefix);
        }

        return _fileStorage.GetPublicUrl(trimmed.TrimStart('/'));
    }

    private static BulkUpdateGroupDto NormalizeStoreMediaValues(BulkUpdateGroupDto request)
    {
        var normalized = request.Items.Select(item =>
        {
            if (!StoreMediaKeys.Contains(item.Key))
                return item;

            var normalizedValue = NormalizeStoreMediaValue(item.Value);
            return item with { Value = normalizedValue };
        }).ToList();

        return new BulkUpdateGroupDto { Items = normalized };
    }

    private static string NormalizeStoreMediaValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var trimmed = value.Trim();

        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var absolute))
        {
            var path = absolute.AbsolutePath;
            if (path.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
                return path.TrimStart('/');
            return path.StartsWith("/") ? $"uploads{path}" : $"uploads/{path}";
        }

        if (trimmed.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
            return trimmed.TrimStart('/');

        if (trimmed.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase))
            return trimmed;

        if (trimmed.StartsWith("/"))
        {
            if (trimmed.StartsWith("/store-media/", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("/store-videos/", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("/store-logo/", StringComparison.OrdinalIgnoreCase))
            {
                return "uploads" + trimmed;
            }

            return trimmed.TrimStart('/');
        }

        if (trimmed.StartsWith("store-media/", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("store-videos/", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("store-logo/", StringComparison.OrdinalIgnoreCase))
        {
            return "uploads/" + trimmed;
        }

        return trimmed;
    }
}
