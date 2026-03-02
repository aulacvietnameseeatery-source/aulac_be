using Core.Entity;
using Core.Interface.Repo;
using Infa.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infa.Repo;

/// <summary>
/// Repository implementation for lookup value data access operations.
/// </summary>
public class LookupRepo : ILookupRepo
{
    private readonly RestaurantMgmtContext _context;
    private readonly ILogger<LookupRepo> _logger;

    public LookupRepo(RestaurantMgmtContext context, ILogger<LookupRepo> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, uint>> LoadAllAsync(CancellationToken ct = default)
    {
        var lookupValues = await _context.LookupValues
            .AsNoTracking()
            .Where(lv => lv.IsActive == true && lv.DeletedAt == null)
            .Select(lv => new { lv.TypeId, lv.ValueCode, lv.ValueId })
            .ToListAsync(ct);

        var dictionary = new Dictionary<string, uint>(StringComparer.Ordinal);

        foreach (var lv in lookupValues)
        {
            if (string.IsNullOrWhiteSpace(lv.ValueCode))
            {
                _logger.LogWarning(
                    "Skipping lookup value with null/empty value_code: value_id={ValueId}, type_id={TypeId}",
                    lv.ValueId,
                    lv.TypeId);
                continue;
            }

            var normalizedCode = lv.ValueCode.Trim().ToUpperInvariant();
            var key = $"{lv.TypeId}|{normalizedCode}";

            if (!dictionary.TryAdd(key, lv.ValueId))
            {
                _logger.LogWarning(
                    "Duplicate lookup value detected: type_id={TypeId}, value_code='{ValueCode}'. Using first occurrence.",
                    lv.TypeId,
                    lv.ValueCode);
            }
        }

        _logger.LogDebug("Loaded {Count} lookup values from database", dictionary.Count);
        return dictionary;
    }

    /// <inheritdoc />
    public async Task<DateTime?> GetMaxUpdatedAtAsync(CancellationToken ct = default)
    {
        try
        {
            return await _context.LookupValues
                .Where(lv => lv.DeletedAt == null)
                .MaxAsync(lv => lv.UpdateAt, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get max UpdateAt timestamp");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<bool> HasUpdatedAtColumnAsync(CancellationToken ct = default)
    {
        try
        {
            await _context.LookupValues
                .AsNoTracking()
                .Select(lv => lv.UpdateAt)
                .FirstOrDefaultAsync(ct);

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<List<LookupValue>> GetAllActiveByTypeAsync(ushort typeId, CancellationToken ct = default)
    {
        return await _context.LookupValues
            .AsNoTracking()
            .Where(lv => lv.TypeId == typeId && lv.IsActive == true && lv.DeletedAt == null)
            .Include(lv => lv.ValueNameText)
                .ThenInclude(t => t!.I18nTranslations)
            .Include(lv => lv.ValueDescText)
                .ThenInclude(t => t!.I18nTranslations)
            .OrderBy(lv => lv.SortOrder)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<LookupValue?> GetByIdAsync(uint valueId, CancellationToken ct = default)
    {
        return await _context.LookupValues
            .FirstOrDefaultAsync(lv => lv.ValueId == valueId && lv.DeletedAt == null, ct);
    }

    /// <inheritdoc />
    public async Task<LookupValue?> GetByIdWithI18nAsync(uint valueId, CancellationToken ct = default)
    {
        return await _context.LookupValues
            .Include(lv => lv.ValueNameText)
                .ThenInclude(t => t!.I18nTranslations)
            .Include(lv => lv.ValueDescText)
                .ThenInclude(t => t!.I18nTranslations)
            .FirstOrDefaultAsync(lv => lv.ValueId == valueId && lv.DeletedAt == null, ct);
    }

    /// <inheritdoc />
    public async Task<bool> ValueNameExistsAsync(ushort typeId, string valueName, uint? excludeId = null, CancellationToken ct = default)
    {
        var query = _context.LookupValues
            .AsNoTracking()
            .Where(lv => lv.TypeId == typeId && lv.ValueName == valueName && lv.DeletedAt == null);

        if (excludeId.HasValue)
            query = query.Where(lv => lv.ValueId != excludeId.Value);

        return await query.AnyAsync(ct);
    }

    /// <inheritdoc />
    public async Task<bool> ValueCodeExistsAsync(ushort typeId, string valueCode, uint? excludeId = null, CancellationToken ct = default)
    {
        var query = _context.LookupValues
            .AsNoTracking()
            .Where(lv => lv.TypeId == typeId && lv.ValueCode == valueCode && lv.DeletedAt == null);

        if (excludeId.HasValue)
            query = query.Where(lv => lv.ValueId != excludeId.Value);

        return await query.AnyAsync(ct);
    }

    /// <inheritdoc />
    public async Task<short> GetMaxSortOrderAsync(ushort typeId, CancellationToken ct = default)
    {
        var hasAny = await _context.LookupValues
            .AsNoTracking()
            .Where(lv => lv.TypeId == typeId && lv.DeletedAt == null)
            .AnyAsync(ct);

        if (!hasAny) return 0;

        return await _context.LookupValues
            .AsNoTracking()
            .Where(lv => lv.TypeId == typeId && lv.DeletedAt == null)
            .MaxAsync(lv => lv.SortOrder, ct);
    }

    /// <inheritdoc />
    public void Add(LookupValue entity)
    {
        _context.LookupValues.Add(entity);
    }

    /// <inheritdoc />
    public async Task<int> CountTablesUsingLookupValueAsync(uint valueId, CancellationToken ct = default)
    {
        return await _context.RestaurantTables
            .AsNoTracking()
            .Where(t => !t.IsDeleted)
            .Where(t => t.ZoneLvId == valueId
                || t.TableTypeLvId == valueId
                || t.TableStatusLvId == valueId)
            .CountAsync(ct);
    }
}
