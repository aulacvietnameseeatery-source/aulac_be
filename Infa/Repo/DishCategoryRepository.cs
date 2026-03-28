using Core.DTO.DishCategory;
using Core.DTO.General;
using Core.Entity;
using Core.Interface.Repo;
using Infa.Data;
using Microsoft.EntityFrameworkCore;

namespace Infa.Repo;

/// <summary>
/// Repository implementation for dish category data access operations.
/// </summary>
public class DishCategoryRepository : IDishCategoryRepository
{
    private readonly RestaurantMgmtContext _context;

    public DishCategoryRepository(RestaurantMgmtContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<PagedResultDTO<DishCategoryDto>> GetAllCategoriesAsync(DishCategoryListQueryDTO query, CancellationToken cancellationToken = default)
    {
        var dbQuery = _context.DishCategories.AsQueryable();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var searchLower = query.Search.ToLower();
            dbQuery = dbQuery.Where(c => 
                c.CategoryName.ToLower().Contains(searchLower) ||
                (c.Description != null && c.Description.ToLower().Contains(searchLower))
            );
        }

        // Apply status filter
        if (query.IsDisabled.HasValue)
        {
            dbQuery = dbQuery.Where(c => c.IsDisabled == query.IsDisabled.Value);
        }

        // Get total count
        var totalCount = await dbQuery.CountAsync(cancellationToken);

        // Apply pagination and eager-load translations
        var categories = await dbQuery
            .OrderBy(c => c.CategoryId)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .Include(c => c.CategoryNameText).ThenInclude(t => t.I18nTranslations)
            .Include(c => c.DescriptionText).ThenInclude(t => t.I18nTranslations)
            .ToListAsync(cancellationToken);

        var pageDtos = categories.Select(c => MapToDto(c)).ToList();

        return new PagedResultDTO<DishCategoryDto>
        {
            PageData = pageDtos,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };
    }

    /// <inheritdoc />
    public async Task<DishCategory?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _context.DishCategories
            .Include(c => c.CategoryNameText).ThenInclude(t => t.I18nTranslations)
            .Include(c => c.DescriptionText).ThenInclude(t => t.I18nTranslations)
            .FirstOrDefaultAsync(c => c.CategoryId == id, cancellationToken);
    }

    // ---------- helpers ----------

    private static DishCategoryDto MapToDto(DishCategory c)
    {
        return new DishCategoryDto
        {
            CategoryId = c.CategoryId,
            CategoryName = c.CategoryName,
            Description = c.Description,
            IsDisabled = c.IsDisabled,
            NameI18n = MapI18nText(c.CategoryNameText, c.CategoryName),
            DescriptionI18n = MapI18nText(c.DescriptionText, c.Description ?? string.Empty),
        };
    }

    private static Core.DTO.Dish.I18nTextDto MapI18nText(Core.Entity.I18nText? text, string fallback)
    {
        if (text == null || text.I18nTranslations == null || !text.I18nTranslations.Any())
            return new Core.DTO.Dish.I18nTextDto { Vi = fallback, En = fallback, Fr = fallback };

        return new Core.DTO.Dish.I18nTextDto
        {
            Vi = text.I18nTranslations.FirstOrDefault(t => t.LangCode == "vi")?.TranslatedText ?? fallback,
            En = text.I18nTranslations.FirstOrDefault(t => t.LangCode == "en")?.TranslatedText ?? fallback,
            Fr = text.I18nTranslations.FirstOrDefault(t => t.LangCode == "fr")?.TranslatedText ?? fallback,
        };
    }

    /// <inheritdoc />
    public async Task<DishCategory> CreateAsync(DishCategory category, CancellationToken cancellationToken = default)
    {
        _context.DishCategories.Add(category);
        await _context.SaveChangesAsync(cancellationToken);
        return category;
    }

    /// <inheritdoc />
    public async Task<DishCategory> UpdateAsync(DishCategory category, CancellationToken cancellationToken = default)
    {
        _context.DishCategories.Update(category);
        await _context.SaveChangesAsync(cancellationToken);
        return category;
    }

    /// <inheritdoc />
   

    /// <inheritdoc />
    public async Task<bool> HasDishesAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _context.Dishes
            .AnyAsync(d => d.CategoryId == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsByNameAsync(string name, long? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.DishCategories
            .Where(c => c.CategoryName.ToLower() == name.ToLower());

        if (excludeId.HasValue)
        {
            query = query.Where(c => c.CategoryId != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> GetMaxDisplayOrderAsync(CancellationToken cancellationToken = default)
    {
        if (!await _context.DishCategories.AnyAsync(cancellationToken))
            return 0;

        return await _context.DishCategories.MaxAsync(c => c.DisPlayOrder, cancellationToken);
    }
}
