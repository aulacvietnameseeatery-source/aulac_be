using Core.Entity;
using Core.Interface.Repo;
using Core.Interface.Service.Others;
using Core.Interface.Service.Entity;
using System.Text.Json;

namespace Core.Service;

/// <summary>
/// Service implementation for system settings management.
/// Provides business logic, caching, and type-safe access to configuration.
/// </summary>
public class SystemSettingService : ISystemSettingService
{
    private readonly ISystemSettingRepository _repository;
    private readonly ICacheService _cacheService;
    private const string CacheKeyPrefix = "system_setting:";
    private const int CacheExpirationMinutes = 60; // Cache for 1 hour

    public SystemSettingService(
        ISystemSettingRepository repository,
        ICacheService cacheService)
    {
        _repository = repository;
        _cacheService = cacheService;
    }

    /// <inheritdoc />
    public async Task<string?> GetStringAsync(
        string key,
        string? defaultValue = null,
        CancellationToken cancellationToken = default)
    {
        var setting = await GetSettingWithCacheAsync(key, cancellationToken);

        if (setting == null || setting.ValueType != "STRING")
        {
            return defaultValue;
        }

        return setting.ValueString ?? defaultValue;
    }

    /// <inheritdoc />
    public async Task<long?> GetIntAsync(
        string key,
        long? defaultValue = null,
        CancellationToken cancellationToken = default)
    {
        var setting = await GetSettingWithCacheAsync(key, cancellationToken);

        if (setting == null || setting.ValueType != "INT")
        {
            return defaultValue;
        }

        return setting.ValueInt ?? defaultValue;
    }

    /// <inheritdoc />
    public async Task<decimal?> GetDecimalAsync(
        string key,
        decimal? defaultValue = null,
        CancellationToken cancellationToken = default)
    {
        var setting = await GetSettingWithCacheAsync(key, cancellationToken);

        if (setting == null || setting.ValueType != "DECIMAL")
        {
            return defaultValue;
        }

        return setting.ValueDecimal ?? defaultValue;
    }

    /// <inheritdoc />
    public async Task<bool?> GetBoolAsync(
        string key,
        bool? defaultValue = null,
        CancellationToken cancellationToken = default)
    {
        var setting = await GetSettingWithCacheAsync(key, cancellationToken);

        if (setting == null || setting.ValueType != "BOOL")
        {
            return defaultValue;
        }

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
        {
            return defaultValue;
        }

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
        string key,
        string value,
        string? description = null,
        bool isSensitive = false,
        long? updatedBy = null,
        CancellationToken cancellationToken = default)
    {
        var setting = new SystemSetting
        {
            SettingKey = key,
            ValueType = "STRING",
            ValueString = value,
            Description = description,
            IsSensitive = isSensitive,
            UpdatedBy = updatedBy
        };

        await _repository.SaveAsync(setting, cancellationToken);
        await ClearCacheAsync(key);
    }

    /// <inheritdoc />
    public async Task SetIntAsync(
            string key,
            long value,
            string? description = null,
            bool isSensitive = false,
            long? updatedBy = null,
            CancellationToken cancellationToken = default)
    {
        var setting = new SystemSetting
        {
            SettingKey = key,
            ValueType = "INT",
            ValueInt = value,
            Description = description,
            IsSensitive = isSensitive,
            UpdatedBy = updatedBy
        };

        await _repository.SaveAsync(setting, cancellationToken);
        await ClearCacheAsync(key);
    }

    /// <inheritdoc />
    public async Task SetDecimalAsync(
        string key,
        decimal value,
        string? description = null,
        bool isSensitive = false,
        long? updatedBy = null,
        CancellationToken cancellationToken = default)
    {
        var setting = new SystemSetting
        {
            SettingKey = key,
            ValueType = "DECIMAL",
            ValueDecimal = value,
            Description = description,
            IsSensitive = isSensitive,
            UpdatedBy = updatedBy
        };

        await _repository.SaveAsync(setting, cancellationToken);
        await ClearCacheAsync(key);
    }

    /// <inheritdoc />
    public async Task SetBoolAsync(
        string key,
        bool value,
        string? description = null,
        bool isSensitive = false,
        long? updatedBy = null,
        CancellationToken cancellationToken = default)
    {
        var setting = new SystemSetting
        {
            SettingKey = key,
            ValueType = "BOOL",
            ValueBool = value,
            Description = description,
            IsSensitive = isSensitive,
            UpdatedBy = updatedBy
        };

        await _repository.SaveAsync(setting, cancellationToken);
        await ClearCacheAsync(key);
    }

    /// <inheritdoc />
    public async Task SetJsonAsync<T>(
        string key,
        T value,
        string? description = null,
        bool isSensitive = false,
        long? updatedBy = null,
        CancellationToken cancellationToken = default)
    {
        var jsonValue = JsonSerializer.Serialize(value);

        var setting = new SystemSetting
        {
            SettingKey = key,
            ValueType = "JSON",
            ValueJson = jsonValue,
            Description = description,
            IsSensitive = isSensitive,
            UpdatedBy = updatedBy
        };

        await _repository.SaveAsync(setting, cancellationToken);
        await ClearCacheAsync(key);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        var result = await _repository.DeleteAsync(key, cancellationToken);
        if (result)
        {
            await ClearCacheAsync(key);
        }
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
            object? value = setting.ValueType switch
            {
                "STRING" => setting.ValueString,
                "INT" => setting.ValueInt,
                "DECIMAL" => setting.ValueDecimal,
                "BOOL" => setting.ValueBool,
                "JSON" => setting.ValueJson,
                _ => null
            };

            result[setting.SettingKey] = value;
        }

        return result;
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

    #region Private Helper Methods

    /// <summary>
    /// Gets a setting from cache or database.
    /// Implements cache-aside pattern.
    /// </summary>
    private async Task<SystemSetting?> GetSettingWithCacheAsync(
        string key,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"{CacheKeyPrefix}{key}";

        // Try to get from cache first
        var cachedSetting = await _cacheService.GetAsync<SystemSetting>(cacheKey);
        if (cachedSetting != null)
        {
            return cachedSetting;
        }

        // If not in cache, get from database
        var setting = await _repository.GetByKeyAsync(key, cancellationToken);
        if (setting != null)
        {
            // Store in cache for future requests
            await _cacheService.SetAsync(
                cacheKey,
                setting,
                TimeSpan.FromMinutes(CacheExpirationMinutes));
        }

        return setting;
    }

    #endregion
}
