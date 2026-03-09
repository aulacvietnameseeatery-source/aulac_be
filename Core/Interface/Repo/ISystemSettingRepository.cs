using Core.Entity;

namespace Core.Interface.Repo;

/// <summary>
/// Repository abstraction for system settings management.
/// Provides access to application configuration stored in the database.
/// </summary>
public interface ISystemSettingRepository
{
    /// <summary>
    /// Retrieves a system setting by its unique key.
    /// </summary>
    Task<SystemSetting?> GetByKeyAsync(
        string settingKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all system settings.
    /// </summary>
    Task<IEnumerable<SystemSetting>> GetAllAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all non-sensitive system settings.
    /// Excludes settings marked as sensitive (e.g., passwords, API keys).
    /// </summary>
    Task<IEnumerable<SystemSetting>> GetAllNonSensitiveAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all settings whose key starts with "{groupPrefix}."
    /// Used to load all settings in a named group (including sensitive ones).
    /// </summary>
    Task<IEnumerable<SystemSetting>> GetByGroupPrefixAsync(
        string groupPrefix,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates a system setting.
    /// If the setting key exists, it updates; otherwise, it creates a new setting.
    /// </summary>
    Task SaveAsync(
        SystemSetting setting,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a system setting by its key.
    /// </summary>
    /// <returns>True if the setting was deleted; otherwise false</returns>
    Task<bool> DeleteAsync(
        string settingKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a system setting exists by its key.
    /// </summary>
    /// <returns>True if the setting exists; otherwise false</returns>
    Task<bool> ExistsAsync(
        string settingKey,
        CancellationToken cancellationToken = default);
}
