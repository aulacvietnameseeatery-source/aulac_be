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
/// <param name="settingKey">The unique identifier for the setting</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The system setting if found; otherwise null</returns>
    Task<SystemSetting?> GetByKeyAsync(
   string settingKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all system settings.
    /// </summary>
 /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of all system settings</returns>
    Task<IEnumerable<SystemSetting>> GetAllAsync(
   CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all non-sensitive system settings.
    /// Excludes settings marked as sensitive (e.g., passwords, API keys).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of non-sensitive system settings</returns>
    Task<IEnumerable<SystemSetting>> GetAllNonSensitiveAsync(
     CancellationToken cancellationToken = default);

 /// <summary>
    /// Creates or updates a system setting.
    /// If the setting key exists, it updates; otherwise, it creates a new setting.
    /// </summary>
    /// <param name="setting">The system setting to save</param>
/// <param name="cancellationToken">Cancellation token</param>
    Task SaveAsync(
 SystemSetting setting,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a system setting by its key.
    /// </summary>
    /// <param name="settingKey">The unique identifier for the setting</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the setting was deleted; otherwise false</returns>
    Task<bool> DeleteAsync(
  string settingKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a system setting exists by its key.
    /// </summary>
    /// <param name="settingKey">The unique identifier for the setting</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the setting exists; otherwise false</returns>
    Task<bool> ExistsAsync(
string settingKey,
        CancellationToken cancellationToken = default);
}
