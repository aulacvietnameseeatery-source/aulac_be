using Core.Entity;
using Core.Interface.Repo;
using Infa.Data;
using Microsoft.EntityFrameworkCore;

namespace Infa.Repo;

/// <summary>
/// EF Core implementation of ISystemSettingRepository.
/// Provides data access for system settings stored in the database.
/// </summary>
public class SystemSettingRepository : ISystemSettingRepository
{
    private readonly RestaurantMgmtContext _context;

    public SystemSettingRepository(RestaurantMgmtContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<SystemSetting?> GetByKeyAsync(
        string settingKey,
        CancellationToken cancellationToken = default)
    {
        return await _context.SystemSettings
              .AsNoTracking()
              .FirstOrDefaultAsync(s => s.SettingKey == settingKey, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<SystemSetting>> GetAllAsync(
          CancellationToken cancellationToken = default)
    {
        return await _context.SystemSettings
            .AsNoTracking()
            .OrderBy(s => s.SettingKey)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<SystemSetting>> GetAllNonSensitiveAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.SystemSettings
            .AsNoTracking()
            .Where(s => !s.IsSensitive)
            .OrderBy(s => s.SettingKey)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task SaveAsync(
        SystemSetting setting,
        CancellationToken cancellationToken = default)
    {
        // Check if setting already exists
        var existing = await _context.SystemSettings
            .FirstOrDefaultAsync(s => s.SettingKey == setting.SettingKey, cancellationToken);

        if (existing != null)
        {
            // Update existing setting
            existing.ValueType = setting.ValueType;
            existing.ValueString = setting.ValueString;
            existing.ValueInt = setting.ValueInt;
            existing.ValueDecimal = setting.ValueDecimal;
            existing.ValueBool = setting.ValueBool;
            existing.ValueJson = setting.ValueJson;
            existing.Description = setting.Description;
            existing.IsSensitive = setting.IsSensitive;
            existing.UpdatedBy = setting.UpdatedBy;
            existing.UpdatedAt = DateTime.UtcNow;

            _context.SystemSettings.Update(existing);
        }
        else
        {
            // Create new setting
            setting.UpdatedAt = DateTime.UtcNow;
            await _context.SystemSettings.AddAsync(setting, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(
        string settingKey,
        CancellationToken cancellationToken = default)
    {
        var setting = await _context.SystemSettings
                 .FirstOrDefaultAsync(s => s.SettingKey == settingKey, cancellationToken);

        if (setting == null)
        {
            return false;
        }

        _context.SystemSettings.Remove(setting);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(
        string settingKey,
        CancellationToken cancellationToken = default)
    {
        return await _context.SystemSettings
                  .AnyAsync(s => s.SettingKey == settingKey, cancellationToken);
    }
}
