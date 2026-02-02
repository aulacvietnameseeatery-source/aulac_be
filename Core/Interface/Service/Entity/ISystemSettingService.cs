namespace Core.Interface.Service.Entity;

/// <summary>
/// Service abstraction for system settings management.
/// Provides business logic and caching for application configuration.
/// </summary>
public interface ISystemSettingService
{
    /// <summary>
  /// Retrieves a string value from system settings.
    /// </summary>
    /// <param name="key">The setting key</param>
    /// <param name="defaultValue">Default value if setting not found</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The setting value or default value</returns>
    Task<string?> GetStringAsync(
        string key,
        string? defaultValue = null,
        CancellationToken cancellationToken = default);

    /// <summary>
  /// Retrieves an integer value from system settings.
    /// </summary>
    /// <param name="key">The setting key</param>
    /// <param name="defaultValue">Default value if setting not found</param>
  /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The setting value or default value</returns>
    Task<long?> GetIntAsync(
        string key,
        long? defaultValue = null,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a decimal value from system settings.
    /// </summary>
    /// <param name="key">The setting key</param>
    /// <param name="defaultValue">Default value if setting not found</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The setting value or default value</returns>
    Task<decimal?> GetDecimalAsync(
        string key,
        decimal? defaultValue = null,
   CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a boolean value from system settings.
    /// </summary>
    /// <param name="key">The setting key</param>
    /// <param name="defaultValue">Default value if setting not found</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The setting value or default value</returns>
    Task<bool?> GetBoolAsync(
        string key,
        bool? defaultValue = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a JSON value from system settings and deserializes it.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to</typeparam>
    /// <param name="key">The setting key</param>
    /// <param name="defaultValue">Default value if setting not found</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The deserialized value or default value</returns>
    Task<T?> GetJsonAsync<T>(
    string key,
        T? defaultValue = default,
    CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a string value in system settings.
    /// </summary>
    /// <param name="key">The setting key</param>
    /// <param name="value">The value to set</param>
    /// <param name="description">Optional description of the setting</param>
    /// <param name="isSensitive">Whether the setting contains sensitive data</param>
    /// <param name="updatedBy">The user ID who updated the setting</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SetStringAsync(
        string key,
        string value,
    string? description = null,
   bool isSensitive = false,
        long? updatedBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets an integer value in system settings.
    /// </summary>
    /// <param name="key">The setting key</param>
    /// <param name="value">The value to set</param>
  /// <param name="description">Optional description of the setting</param>
  /// <param name="isSensitive">Whether the setting contains sensitive data</param>
    /// <param name="updatedBy">The user ID who updated the setting</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SetIntAsync(
        string key,
        long value,
     string? description = null,
        bool isSensitive = false,
     long? updatedBy = null,
 CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a decimal value in system settings.
    /// </summary>
    /// <param name="key">The setting key</param>
    /// <param name="value">The value to set</param>
    /// <param name="description">Optional description of the setting</param>
    /// <param name="isSensitive">Whether the setting contains sensitive data</param>
    /// <param name="updatedBy">The user ID who updated the setting</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SetDecimalAsync(
        string key,
        decimal value,
    string? description = null,
        bool isSensitive = false,
        long? updatedBy = null,
  CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a boolean value in system settings.
    /// </summary>
    /// <param name="key">The setting key</param>
    /// <param name="value">The value to set</param>
    /// <param name="description">Optional description of the setting</param>
    /// <param name="isSensitive">Whether the setting contains sensitive data</param>
    /// <param name="updatedBy">The user ID who updated the setting</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SetBoolAsync(
        string key,
 bool value,
        string? description = null,
     bool isSensitive = false,
        long? updatedBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a JSON value in system settings.
    /// </summary>
    /// <typeparam name="T">The type to serialize</typeparam>
    /// <param name="key">The setting key</param>
    /// <param name="value">The value to serialize and set</param>
    /// <param name="description">Optional description of the setting</param>
    /// <param name="isSensitive">Whether the setting contains sensitive data</param>
    /// <param name="updatedBy">The user ID who updated the setting</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SetJsonAsync<T>(
        string key,
        T value,
        string? description = null,
     bool isSensitive = false,
 long? updatedBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a system setting by its key.
    /// </summary>
    /// <param name="key">The setting key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted successfully; otherwise false</returns>
    Task<bool> DeleteAsync(
        string key,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all non-sensitive system settings.
    /// Useful for admin configuration UIs.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary of key-value pairs</returns>
    Task<Dictionary<string, object?>> GetAllNonSensitiveAsync(
CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears the cache for a specific setting key.
    /// Should be called after updating a setting to ensure fresh data.
/// </summary>
 /// <param name="key">The setting key</param>
    Task ClearCacheAsync(string key);

    /// <summary>
    /// Clears all system settings from cache.
    /// </summary>
    Task ClearAllCacheAsync();
}
