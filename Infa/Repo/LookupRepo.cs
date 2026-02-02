using Core.Interface.Repo;
using Infa.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infa.Repo;

/// <summary>
/// SCOPED service for loading lookup data from database via DbContext.
/// Handles all database access operations for lookup values.
/// </summary>
public class LookupRepo : ILookupRepo
{
    private readonly RestaurantMgmtContext _context;
    private readonly ILogger<LookupRepo> _logger;

    public LookupRepo(
        RestaurantMgmtContext context,
        ILogger<LookupRepo> logger)
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
      .Select(lv => new
      {
          lv.TypeId,
          lv.ValueCode,
          lv.ValueId
      })
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

            // Normalize: Trim + ToUpperInvariant
            var normalizedCode = lv.ValueCode.Trim().ToUpperInvariant();
            var key = BuildCacheKey(lv.TypeId, normalizedCode);

            // Handle duplicates: keep first, log warning
            if (dictionary.ContainsKey(key))
            {
                _logger.LogWarning(
                    "Duplicate lookup value detected: type_id={TypeId}, value_code='{ValueCode}'. Using first occurrence.",
                    lv.TypeId,
                    lv.ValueCode
                    );
                continue;
            }

            dictionary[key] = lv.ValueId;
        }

        _logger.LogDebug("Loaded {Count} lookup values from database", dictionary.Count);
        return dictionary;
    }

    /// <inheritdoc />
    public async Task<DateTime?> GetMaxUpdatedAtAsync(CancellationToken ct = default)
    {
        try
        {
            var maxUpdatedAt = await _context.LookupValues
                .Where(lv => lv.DeletedAt == null)
                .MaxAsync(lv => lv.UpdateAt, ct);

            return maxUpdatedAt;
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
            // Try to query UpdateAt column
            await _context.LookupValues
            .AsNoTracking()
            .Select(lv => lv.UpdateAt)
            .FirstOrDefaultAsync(ct);

            return true;
        }
        catch (Exception)
        {
            // Column doesn't exist or other error
            return false;
        }
    }

    /// <summary>
    /// Builds a cache key from type_id and normalized value_code.
    /// Format: "{typeId}|{normalizedValueCode}"
    /// </summary>
    private static string BuildCacheKey(ushort typeId, string normalizedValueCode)
    {
        return $"{typeId}|{normalizedValueCode}";
    }
}
