using Core.DTO.Dish;
using Core.DTO.DishCategory;
using Core.DTO.General;
using Core.Entity;
using Core.Exceptions;
using Core.Interface.Repo;
using Core.Interface.Service.Entity;
using Core.Interface.Service.I18n;
using Microsoft.Extensions.Logging;

namespace Core.Service;

/// <summary>
/// Implementation of IDishCategoryService handling dish category business logic
/// </summary>
public class DishCategoryService : IDishCategoryService
{
    private readonly IDishCategoryRepository _dishCategoryRepository;
    private readonly Ii18nService _i18nService;
    private readonly ILogger<DishCategoryService> _logger;

    public DishCategoryService(
        IDishCategoryRepository dishCategoryRepository,
        Ii18nService i18nService,
        ILogger<DishCategoryService> logger)
    {
        _dishCategoryRepository = dishCategoryRepository;
        _i18nService = i18nService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<PagedResultDTO<DishCategoryDto>> GetAllCategoriesAsync(DishCategoryListQueryDTO query, CancellationToken cancellationToken = default)
    {
        return await _dishCategoryRepository.GetAllCategoriesAsync(query, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<DishCategoryDto?> GetCategoryByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var category = await _dishCategoryRepository.GetByIdAsync(id, cancellationToken);
        
        return category == null ? null : MapToDto(category);
    }

    /// <inheritdoc />
    public async Task<DishCategoryDto> CreateCategoryAsync(CreateDishCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var englishName = request.I18n.GetValueOrDefault("en")?.Name ?? string.Empty;

        var nameExists = await _dishCategoryRepository.ExistsByNameAsync(englishName, null, cancellationToken);
        if (nameExists)
        {
            _logger.LogWarning("Cannot create category: name '{CategoryName}' already exists", englishName);
            throw new ConflictException($"Category name '{englishName}' already exists.");
        }

        // Build name translations
        var nameTranslations = request.I18n
            .Where(kv => !string.IsNullOrWhiteSpace(kv.Value.Name))
            .ToDictionary(kv => kv.Key, kv => kv.Value.Name);

        var nameTextId = await _i18nService.CreateAsync(
            $"dish_category.name.{Guid.NewGuid():N}",
            "Dish Category Name",
            "en",
            nameTranslations,
            cancellationToken);

        // Build description translations (optional)
        var descTranslations = request.I18n
            .Where(kv => !string.IsNullOrWhiteSpace(kv.Value.Description))
            .ToDictionary(kv => kv.Key, kv => kv.Value.Description!);

        long? descTextId = null;
        if (descTranslations.Any())
        {
            descTextId = await _i18nService.CreateAsync(
                $"dish_category.description.{Guid.NewGuid():N}",
                "Dish Category Description",
                "en",
                descTranslations,
                cancellationToken);
        }

        var englishDesc = request.I18n.GetValueOrDefault("en")?.Description;

        var category = new DishCategory
        {
            CategoryName = englishName,
            Description = englishDesc,
            IsDisabled = request.IsDisabled,
            CategoryNameTextId = nameTextId,
            DescriptionTextId = descTextId,
            DisPlayOrder = await _dishCategoryRepository.GetMaxDisplayOrderAsync(cancellationToken) + 1,
        };

        var created = await _dishCategoryRepository.CreateAsync(category, cancellationToken);
        
        _logger.LogInformation("Created new dish category: {CategoryName} (ID: {CategoryId})", 
            created.CategoryName, created.CategoryId);

        // Re-fetch with translations for the response
        var createdWithTranslations = await _dishCategoryRepository.GetByIdAsync(created.CategoryId, cancellationToken);
        return MapToDto(createdWithTranslations!);
    }

    /// <inheritdoc />
    public async Task<DishCategoryDto> UpdateCategoryAsync(long id, UpdateDishCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var category = await _dishCategoryRepository.GetByIdAsync(id, cancellationToken);
        
        if (category == null)
        {
            _logger.LogWarning("Dish category with ID {CategoryId} not found", id);
            throw new KeyNotFoundException($"Dish category with ID {id} not found.");
        }

        var englishName = request.I18n.GetValueOrDefault("en")?.Name ?? string.Empty;

        var nameExists = await _dishCategoryRepository.ExistsByNameAsync(englishName, id, cancellationToken);
        if (nameExists)
        {
            _logger.LogWarning("Cannot update category: name '{CategoryName}' already exists", englishName);
            throw new ConflictException($"Category name '{englishName}' already exists.");
        }

        // --- Update or create name i18n text ---
        var nameTranslations = request.I18n
            .Where(kv => !string.IsNullOrWhiteSpace(kv.Value.Name))
            .ToDictionary(kv => kv.Key, kv => kv.Value.Name);

        if (category.CategoryNameTextId.HasValue)
        {
            await _i18nService.UpdateStringsAsync(category.CategoryNameTextId.Value, nameTranslations, cancellationToken);
        }
        else
        {
            var newNameTextId = await _i18nService.CreateAsync($"dish_category.name.{Guid.NewGuid():N}", "Dish Category Name", "en", nameTranslations, cancellationToken);
            category.CategoryNameTextId = newNameTextId;
        }

        // --- Update or create description i18n text ---
        var descTranslations = request.I18n
            .Where(kv => !string.IsNullOrWhiteSpace(kv.Value.Description))
            .ToDictionary(kv => kv.Key, kv => kv.Value.Description!);

        if (descTranslations.Any())
        {
            if (category.DescriptionTextId.HasValue)
            {
                await _i18nService.UpdateStringsAsync(category.DescriptionTextId.Value, descTranslations, cancellationToken);
            }
            else
            {
                var newDescTextId = await _i18nService.CreateAsync($"dish_category.description.{Guid.NewGuid():N}", "Dish Category Description", "en", descTranslations, cancellationToken);
                category.DescriptionTextId = newDescTextId;
            }
        }

        category.CategoryName = englishName;
        category.Description = request.I18n.GetValueOrDefault("en")?.Description;
        category.IsDisabled = request.IsDisabled;

        await _dishCategoryRepository.UpdateAsync(category, cancellationToken);
        
        _logger.LogInformation("Updated dish category: {CategoryName} (ID: {CategoryId})", 
            category.CategoryName, category.CategoryId);

        // Re-fetch with fresh translations for the response
        var updated = await _dishCategoryRepository.GetByIdAsync(category.CategoryId, cancellationToken);
        return MapToDto(updated!);
    }

    /// <inheritdoc />
    public async Task<DishCategoryDto> ToggleCategoryStatusAsync(long id, bool isDisabled, CancellationToken cancellationToken = default)
    {
        var category = await _dishCategoryRepository.GetByIdAsync(id, cancellationToken);
        
        if (category == null)
        {
            _logger.LogWarning("Dish category with ID {CategoryId} not found", id);
            throw new KeyNotFoundException($"Dish category with ID {id} not found.");
        }

        category.IsDisabled = isDisabled;
        await _dishCategoryRepository.UpdateAsync(category, cancellationToken);
        
        _logger.LogInformation("Toggled dish category status: {CategoryName} (ID: {CategoryId}) - IsDisabled: {IsDisabled}", 
            category.CategoryName, category.CategoryId, isDisabled);
        
        return MapToDto(category);
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

    private static I18nTextDto MapI18nText(I18nText? text, string fallback)
    {
        if (text == null || text.I18nTranslations == null || !text.I18nTranslations.Any())
            return new I18nTextDto { Vi = fallback, En = fallback, Fr = fallback };

        return new I18nTextDto
        {
            Vi = text.I18nTranslations.FirstOrDefault(t => t.LangCode == "vi")?.TranslatedText ?? fallback,
            En = text.I18nTranslations.FirstOrDefault(t => t.LangCode == "en")?.TranslatedText ?? fallback,
            Fr = text.I18nTranslations.FirstOrDefault(t => t.LangCode == "fr")?.TranslatedText ?? fallback,
        };
    }
}

