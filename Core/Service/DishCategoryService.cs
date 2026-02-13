using Core.DTO.DishCategory;
using Core.DTO.General;
using Core.Entity;
using Core.Interface.Repo;
using Core.Interface.Service.Entity;
using Microsoft.Extensions.Logging;

namespace Core.Service;

/// <summary>
/// Implementation of IDishCategoryService handling dish category business logic
/// </summary>
public class DishCategoryService : IDishCategoryService
{
    private readonly IDishCategoryRepository _dishCategoryRepository;
    private readonly ILogger<DishCategoryService> _logger;

    public DishCategoryService(
        IDishCategoryRepository dishCategoryRepository,
        ILogger<DishCategoryService> logger)
    {
        _dishCategoryRepository = dishCategoryRepository;
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
        // Check if category name already exists
        var nameExists = await _dishCategoryRepository.ExistsByNameAsync(request.CategoryName, null, cancellationToken);
        if (nameExists)
        {
            _logger.LogWarning("Cannot create category: name '{CategoryName}' already exists", request.CategoryName);
            throw new InvalidOperationException($"Category name '{request.CategoryName}' already exists.");
        }

        var category = new DishCategory
        {
            CategoryName = request.CategoryName,
            Description = request.Description,
            IsDisabled = request.IsDisabled
        };

        var createdCategory = await _dishCategoryRepository.CreateAsync(category, cancellationToken);
        
        _logger.LogInformation("Created new dish category: {CategoryName} (ID: {CategoryId})", 
            createdCategory.CategoryName, createdCategory.CategoryId);
        
        return MapToDto(createdCategory);
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

        // Check if category name already exists (exclude current category)
        var nameExists = await _dishCategoryRepository.ExistsByNameAsync(request.CategoryName, id, cancellationToken);
        if (nameExists)
        {
            _logger.LogWarning("Cannot update category: name '{CategoryName}' already exists", request.CategoryName);
            throw new InvalidOperationException($"Category name '{request.CategoryName}' already exists.");
        }

        category.CategoryName = request.CategoryName;
        category.Description = request.Description;
        category.IsDisabled = request.IsDisabled;

        var updatedCategory = await _dishCategoryRepository.UpdateAsync(category, cancellationToken);
        
        _logger.LogInformation("Updated dish category: {CategoryName} (ID: {CategoryId})", 
            updatedCategory.CategoryName, updatedCategory.CategoryId);
        
        return MapToDto(updatedCategory);
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
        var updatedCategory = await _dishCategoryRepository.UpdateAsync(category, cancellationToken);
        
        _logger.LogInformation("Toggled dish category status: {CategoryName} (ID: {CategoryId}) - IsDisabled: {IsDisabled}", 
            updatedCategory.CategoryName, updatedCategory.CategoryId, isDisabled);
        
        return MapToDto(updatedCategory);
    }

    /// <summary>
    /// Maps DishCategory entity to DishCategoryDto
    /// </summary>
    private static DishCategoryDto MapToDto(DishCategory category)
    {
        return new DishCategoryDto
        {
            CategoryId = category.CategoryId,
            CategoryName = category.CategoryName,
            Description = category.Description,
            IsDisabled = category.IsDisabled
        };
    }
}
