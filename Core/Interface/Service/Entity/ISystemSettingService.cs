using Core.DTO.SystemSetting;

namespace Core.Interface.Service.Entity;

/// <summary>
/// Service abstraction for system settings management.
/// Provides business logic and caching for application configuration.
/// </summary>
public interface ISystemSettingService
{
    /// <summary>Retrieves a string value from system settings.</summary>
    Task<string?> GetStringAsync(
        string key,
        string? defaultValue = null,
        CancellationToken cancellationToken = default);

    /// <summary>Retrieves an integer value from system settings.</summary>
    Task<long?> GetIntAsync(
        string key,
        long? defaultValue = null,
        CancellationToken cancellationToken = default);

    /// <summary>Retrieves a decimal value from system settings.</summary>
    Task<decimal?> GetDecimalAsync(
        string key,
        decimal? defaultValue = null,
        CancellationToken cancellationToken = default);

    /// <summary>Retrieves a boolean value from system settings.</summary>
    Task<bool?> GetBoolAsync(
        string key,
        bool? defaultValue = null,
        CancellationToken cancellationToken = default);

    /// <summary>Retrieves a JSON value from system settings and deserializes it.</summary>
    Task<T?> GetJsonAsync<T>(
        string key,
        T? defaultValue = default,
        CancellationToken cancellationToken = default);

    /// <summary>Sets a string value in system settings.</summary>
    Task SetStringAsync(
        string key,
        string value,
        string? description = null,
        bool isSensitive = false,
        long? updatedBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>Sets an integer value in system settings.</summary>
    Task SetIntAsync(
        string key,
        long value,
        string? description = null,
        bool isSensitive = false,
        long? updatedBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>Sets a decimal value in system settings.</summary>
    Task SetDecimalAsync(
        string key,
        decimal value,
        string? description = null,
        bool isSensitive = false,
        long? updatedBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>Sets a boolean value in system settings.</summary>
    Task SetBoolAsync(
        string key,
        bool value,
        string? description = null,
        bool isSensitive = false,
        long? updatedBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>Sets a JSON value in system settings.</summary>
    Task SetJsonAsync<T>(
        string key,
        T value,
        string? description = null,
        bool isSensitive = false,
        long? updatedBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>Creates a new generic system setting with explicit value type.</summary>
    Task CreateSettingAsync(
        string key,
        string? settingName,
        string valueType,
        string value,
        string? description = null,
        bool isSensitive = false,
        long? updatedBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>Deletes a system setting by its key.</summary>
    Task<bool> DeleteAsync(
        string key,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all non-sensitive system settings as a flat dictionary.
    /// </summary>
    Task<Dictionary<string, object?>> GetAllNonSensitiveAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all non-sensitive settings grouped by their key prefix.
    /// E.g. key "password.min_length" belongs to group "password".
    /// Settings without a "." prefix go into group "general".
    /// </summary>
    Task<Dictionary<string, List<SystemSettingDetailDto>>> GetAllGroupedAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all settings (including sensitive) for the specified group prefix.
    /// </summary>
    Task<List<SystemSettingDetailDto>> GetGroupAsync(
        string group,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves ONLY non-sensitive settings for the specified group prefix.
    /// Useful for public API endpoints.
    /// </summary>
    Task<List<SystemSettingDetailDto>> GetPublicGroupAsync(
        string group,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk-updates (upserts) all settings in a group.
    /// The value type is inferred from the existing setting; new keys default to STRING.
    /// </summary>
    Task BulkUpdateGroupAsync(
        string group,
        List<BulkUpdateSettingItemDto> items,
        long? updatedBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>Clears the cache for a specific setting key.</summary>
    Task ClearCacheAsync(string key);

    /// <summary>Clears all system settings from cache.</summary>
    Task ClearAllCacheAsync();
}
