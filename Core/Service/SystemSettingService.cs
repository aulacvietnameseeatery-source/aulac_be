using Core.DTO.SystemSetting;
using Core.Entity;
using Core.Interface.Repo;
using Core.Interface.Service.Others;
using Core.Interface.Service.Entity;
using System.Text.Json;
using Core.DTO.General;
using Core.Interface.Service.FileStorage;

namespace Core.Service;

/// <summary>
/// Service implementation for system settings management.
/// Provides business logic, caching, and type-safe access to configuration.
/// </summary>
public class SystemSettingService : ISystemSettingService
{
    private readonly ISystemSettingRepository _repository;
    private readonly ICacheService _cacheService;
    private readonly IFileStorage _fileStorage;
    private const string CacheKeyPrefix = "system_setting:";
    private const int CacheExpirationMinutes = 60;

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

    public SystemSettingService(
        ISystemSettingRepository repository,
        ICacheService cacheService,
        IFileStorage fileStorage)
    {
        _repository = repository;
        _cacheService = cacheService;
        _fileStorage = fileStorage;
    }

    /// <inheritdoc />
    public async Task<string?> GetStringAsync(
        string key,
        string? defaultValue = null,
        CancellationToken cancellationToken = default)
    {
        var setting = await GetSettingWithCacheAsync(key, cancellationToken);
        if (setting == null || setting.ValueType != "STRING") return defaultValue;
        return setting.ValueString ?? defaultValue;
    }

    /// <inheritdoc />
    public async Task<long?> GetIntAsync(
        string key,
        long? defaultValue = null,
        CancellationToken cancellationToken = default)
    {
        var setting = await GetSettingWithCacheAsync(key, cancellationToken);
        if (setting == null || setting.ValueType != "INT") return defaultValue;
        return setting.ValueInt ?? defaultValue;
    }

    /// <inheritdoc />
    public async Task<decimal?> GetDecimalAsync(
        string key,
        decimal? defaultValue = null,
        CancellationToken cancellationToken = default)
    {
        var setting = await GetSettingWithCacheAsync(key, cancellationToken);
        if (setting == null || setting.ValueType != "DECIMAL") return defaultValue;
        return setting.ValueDecimal ?? defaultValue;
    }

    /// <inheritdoc />
    public async Task<bool?> GetBoolAsync(
        string key,
        bool? defaultValue = null,
        CancellationToken cancellationToken = default)
    {
        var setting = await GetSettingWithCacheAsync(key, cancellationToken);
        if (setting == null || setting.ValueType != "BOOL") return defaultValue;
        return setting.ValueBool ?? defaultValue;
    }

    /// <inheritdoc />
    public async Task<T?> GetJsonAsync<T>(
        string key,
        T? defaultValue = default,
        CancellationToken cancellationToken = default)
    {
        var setting = await GetSettingWithCacheAsync(key, cancellationToken);
        if (setting == null || setting.ValueType != "JSON" || string.IsNullOrWhiteSpace(setting.ValueJson))
            return defaultValue;
        try
        {
            return JsonSerializer.Deserialize<T>(setting.ValueJson);
        }
        catch (JsonException)
        {
            return defaultValue;
        }
    }

    /// <inheritdoc />
    public async Task SetStringAsync(
        string key, string value,
        string? description = null, bool isSensitive = false,
        long? updatedBy = null, CancellationToken cancellationToken = default)
    {
        var setting = new SystemSetting
        {
            SettingKey = key, ValueType = "STRING", ValueString = value,
            Description = description, IsSensitive = isSensitive, UpdatedBy = updatedBy
        };
        await _repository.SaveAsync(setting, cancellationToken);
        await ClearCacheAsync(key);
    }

    /// <inheritdoc />
    public async Task SetIntAsync(
        string key, long value,
        string? description = null, bool isSensitive = false,
        long? updatedBy = null, CancellationToken cancellationToken = default)
    {
        var setting = new SystemSetting
        {
            SettingKey = key, ValueType = "INT", ValueInt = value,
            Description = description, IsSensitive = isSensitive, UpdatedBy = updatedBy
        };
        await _repository.SaveAsync(setting, cancellationToken);
        await ClearCacheAsync(key);
    }

    /// <inheritdoc />
    public async Task SetDecimalAsync(
        string key, decimal value,
        string? description = null, bool isSensitive = false,
        long? updatedBy = null, CancellationToken cancellationToken = default)
    {
        var setting = new SystemSetting
        {
            SettingKey = key, ValueType = "DECIMAL", ValueDecimal = value,
            Description = description, IsSensitive = isSensitive, UpdatedBy = updatedBy
        };
        await _repository.SaveAsync(setting, cancellationToken);
        await ClearCacheAsync(key);
    }

    /// <inheritdoc />
    public async Task SetBoolAsync(
        string key, bool value,
        string? description = null, bool isSensitive = false,
        long? updatedBy = null, CancellationToken cancellationToken = default)
    {
        var setting = new SystemSetting
        {
            SettingKey = key, ValueType = "BOOL", ValueBool = value,
            Description = description, IsSensitive = isSensitive, UpdatedBy = updatedBy
        };
        await _repository.SaveAsync(setting, cancellationToken);
        await ClearCacheAsync(key);
    }

    /// <inheritdoc />
    public async Task SetJsonAsync<T>(
        string key, T value,
        string? description = null, bool isSensitive = false,
        long? updatedBy = null, CancellationToken cancellationToken = default)
    {
        var jsonValue = JsonSerializer.Serialize(value);
        var setting = new SystemSetting
        {
            SettingKey = key, ValueType = "JSON", ValueJson = jsonValue,
            Description = description, IsSensitive = isSensitive, UpdatedBy = updatedBy
        };
        await _repository.SaveAsync(setting, cancellationToken);
        await ClearCacheAsync(key);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        var result = await _repository.DeleteAsync(key, cancellationToken);
        if (result) await ClearCacheAsync(key);
        return result;
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, object?>> GetAllNonSensitiveAsync(
        CancellationToken cancellationToken = default)
    {
        var settings = await _repository.GetAllNonSensitiveAsync(cancellationToken);
        var result = new Dictionary<string, object?>();
        foreach (var setting in settings)
        {
            result[setting.SettingKey] = ExtractValue(setting);
        }
        return result;
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, List<SystemSettingDetailDto>>> GetAllGroupedAsync(
        CancellationToken cancellationToken = default)
    {
        var settings = await _repository.GetAllAsync(cancellationToken);
        var grouped = new Dictionary<string, List<SystemSettingDetailDto>>(StringComparer.OrdinalIgnoreCase);

        foreach (var s in settings)
        {
            var group = ExtractGroup(s.SettingKey);
            if (!grouped.ContainsKey(group))
                grouped[group] = new List<SystemSettingDetailDto>();
            grouped[group].Add(MapToDetailDto(s));
        }

        return grouped;
    }

    /// <inheritdoc />
    public async Task<List<SystemSettingDetailDto>> GetGroupAsync(
        string group,
        CancellationToken cancellationToken = default)
    {
        var settings = await _repository.GetByGroupPrefixAsync(group, cancellationToken);
        var dtos = settings.Select(MapToDetailDto).ToList();

        if (group.Equals("store", StringComparison.OrdinalIgnoreCase))
        {
            dtos = dtos.Select(AttachPublicUrlForStoreMedia).ToList();
        }

        return dtos;
    }

    /// <inheritdoc />
    public async Task<List<SystemSettingDetailDto>> GetPublicGroupAsync(
        string group,
        CancellationToken cancellationToken = default)
    {
        var settings = await _repository.GetByGroupPrefixAsync(group, cancellationToken);
        var dtos = settings
            .Where(s => !s.IsSensitive)
            .Select(MapToDetailDto)
            .ToList();

        if (group.Equals("store", StringComparison.OrdinalIgnoreCase))
        {
            dtos = dtos.Select(AttachPublicUrlForStoreMedia).ToList();
        }

        return dtos;
    }

    /// <inheritdoc />
    public async Task BulkUpdateGroupAsync(
        string group,
        List<BulkUpdateSettingItemDto> items,
        long? updatedBy = null,
        CancellationToken cancellationToken = default)
    {
        if (group.Equals("store", StringComparison.OrdinalIgnoreCase))
        {
            items = NormalizeStoreMediaValues(items);
        }

        // Load existing settings for the group to preserve ValueType and IsSensitive
        var existing = await _repository.GetByGroupPrefixAsync(group, cancellationToken);
        var existingDict = existing.ToDictionary(s => s.SettingKey, StringComparer.OrdinalIgnoreCase);

        foreach (var item in items)
        {
            if (existingDict.TryGetValue(item.Key, out var existingSetting))
            {
                // Update – preserve ValueType and IsSensitive
                var updated = new SystemSetting
                {
                    SettingKey = item.Key,
                    SettingName = item.SettingName ?? existingSetting.SettingName,
                    ValueType = existingSetting.ValueType,
                    IsSensitive = existingSetting.IsSensitive,
                    Description = item.Description ?? existingSetting.Description,
                    UpdatedBy = updatedBy
                };

                var shouldForceJsonRecipients =
                    item.Key.StartsWith("notification.", StringComparison.OrdinalIgnoreCase) &&
                    item.Key.EndsWith(".recipients", StringComparison.OrdinalIgnoreCase);

                if (shouldForceJsonRecipients)
                    updated.ValueType = "JSON";

                // Parse value according to existing type
                switch (updated.ValueType)
                {
                    case "STRING":
                        updated.ValueString = item.Value;
                        break;
                    case "INT":
                        updated.ValueInt = long.TryParse(item.Value, out var l) ? l : existingSetting.ValueInt;
                        break;
                    case "DECIMAL":
                        updated.ValueDecimal = decimal.TryParse(item.Value,
                            System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture,
                            out var d) ? d : existingSetting.ValueDecimal;
                        break;
                    case "BOOL":
                        updated.ValueBool = bool.TryParse(item.Value, out var b) ? b : existingSetting.ValueBool;
                        break;
                    case "JSON":
                        updated.ValueJson = item.Value;
                        break;
                    default:
                        updated.ValueString = item.Value;
                        break;
                }

                await _repository.SaveAsync(updated, cancellationToken);
            }
            else
            {
                // New setting – default to STRING
                var newSetting = new SystemSetting
                {
                    SettingKey = item.Key,
                    SettingName = item.SettingName,
                    ValueType = "STRING",
                    ValueString = item.Value,
                    Description = item.Description,
                    IsSensitive = false,
                    UpdatedBy = updatedBy
                };
                await _repository.SaveAsync(newSetting, cancellationToken);
            }

            await ClearCacheAsync(item.Key);
        }
    }

    /// <inheritdoc />
    public async Task CreateSettingAsync(
        string key, string? settingName, string valueType, string value,
        string? description = null, bool isSensitive = false,
        long? updatedBy = null, CancellationToken cancellationToken = default)
    {
        var setting = new SystemSetting
        {
            SettingKey = key,
            SettingName = settingName,
            ValueType = valueType.ToUpperInvariant(),
            Description = description,
            IsSensitive = isSensitive,
            UpdatedBy = updatedBy
        };

        switch (setting.ValueType)
        {
            case "INT":
                setting.ValueInt = long.TryParse(value, out var l) ? l : 0;
                break;
            case "DECIMAL":
                setting.ValueDecimal = decimal.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : 0m;
                break;
            case "BOOL":
                setting.ValueBool = bool.TryParse(value, out var b) ? b : false;
                break;
            case "JSON":
                setting.ValueJson = value;
                break;
            default:
                setting.ValueType = "STRING";
                setting.ValueString = value;
                break;
        }

        await _repository.SaveAsync(setting, cancellationToken);
        await ClearCacheAsync(key);
    }

    /// <inheritdoc />
    public async Task ClearCacheAsync(string key)
    {
        var cacheKey = $"{CacheKeyPrefix}{key}";
        await _cacheService.RemoveAsync(cacheKey);
    }

    /// <inheritdoc />
    public async Task ClearAllCacheAsync()
    {
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<FileUploadResult> UploadStoreLogoAsync(Stream stream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        var uploadRequest = new FileUploadRequest
        {
            Stream = stream,
            FileName = fileName,
            ContentType = contentType
        };

        return await _fileStorage.SaveAsync(uploadRequest, "store-logo", FileValidationOptions.ImageUpload, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<FileUploadResult> UploadStoreFileAsync(Stream stream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        var uploadRequest = new FileUploadRequest
        {
            Stream = stream,
            FileName = fileName,
            ContentType = contentType
        };

        var extension = Path.GetExtension(fileName);
        var isVideo = contentType.Equals("video/mp4", StringComparison.OrdinalIgnoreCase)
                      || (extension != null && extension.Equals(".mp4", StringComparison.OrdinalIgnoreCase));

        var validation = isVideo ? FileValidationOptions.StoreIntroVideo : FileValidationOptions.ImageUpload;
        var folder = isVideo ? "store-videos" : "store-media";

        return await _fileStorage.SaveAsync(uploadRequest, folder, validation, cancellationToken);
    }

    #region Private Helpers

    private async Task<SystemSetting?> GetSettingWithCacheAsync(
        string key, CancellationToken cancellationToken)
    {
        var cacheKey = $"{CacheKeyPrefix}{key}";
        var cachedSetting = await _cacheService.GetAsync<SystemSetting>(cacheKey);
        if (cachedSetting != null) return cachedSetting;

        var setting = await _repository.GetByKeyAsync(key, cancellationToken);
        if (setting != null)
        {
            await _cacheService.SetAsync(
                cacheKey, setting,
                TimeSpan.FromMinutes(CacheExpirationMinutes));
        }
        return setting;
    }

    private static object? ExtractValue(SystemSetting s) => s.ValueType switch
    {
        "STRING" => s.ValueString,
        "INT" => s.ValueInt,
        "DECIMAL" => s.ValueDecimal,
        "BOOL" => s.ValueBool,
        "JSON" => s.ValueJson,
        _ => null
    };

    private static string ExtractGroup(string settingKey)
    {
        var dotIndex = settingKey.IndexOf('.');
        return dotIndex > 0 ? settingKey[..dotIndex] : "general";
    }

    private static SystemSettingDetailDto MapToDetailDto(SystemSetting s) => new()
    {
        SettingKey = s.SettingKey,
        SettingName = s.SettingName,
        Group = ExtractGroup(s.SettingKey),
        ValueType = s.ValueType,
        Value = ExtractValue(s),
        Description = s.Description,
        IsSensitive = s.IsSensitive,
        UpdatedAt = s.UpdatedAt
    };

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

    private List<BulkUpdateSettingItemDto> NormalizeStoreMediaValues(List<BulkUpdateSettingItemDto> items)
    {
        return items.Select(item =>
        {
            if (!StoreMediaKeys.Contains(item.Key))
                return item;

            var normalizedValue = NormalizeStoreMediaValue(item.Value);
            return item with { Value = normalizedValue };
        }).ToList();
    }

    private string NormalizeStoreMediaValue(string? value)
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

    #endregion
}
