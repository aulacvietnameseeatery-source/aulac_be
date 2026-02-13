
using Core.DTO.Dish;
using Core.Extensions;
using Core.Entity;
using Core.Interface.Repo;
using Core.Interface.Service.Entity;
using Microsoft.Extensions.Logging;

namespace Core.Service;

/// <summary>
/// Implementation of IDishService handling dish business logic
/// Service implementation for dish business logic operations.
/// </summary>
public class DishService : IDishService
{
    private readonly IDishRepository _dishRepository;
    private readonly ILookupResolver _lookupResolver;
    private readonly ILogger<DishService> _logger;

    public DishService(IDishRepository dishRepository, ILookupResolver lookupResolver, ILogger<DishService> logger)
    {
        _dishRepository = dishRepository;
        _lookupResolver = lookupResolver;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<DishDetailDto?> GetDishByIdAsync(long id, string? langCode = null, CancellationToken cancellationToken = default)
    {
        var dish = await _dishRepository.GetDishByIdAsync(id, cancellationToken);
        
        // Default to English if not specified. Supported: en (English), fr (French), vi (Vietnamese)
        var language = langCode ?? "en";

        if (dish == null)
        {
            return null;
        }

        return new DishDetailDto
        {
            DishId = dish.DishId,
            DishName = dish.DishNameText.GetTranslation(language),
            Price = dish.Price,
            CategoryName = dish.Category.CategoryName,
            Description = dish.DescriptionText?.GetTranslation(language),
            ShortDescription = dish.ShortDescriptionText?.GetTranslation(language),
            Slogan = dish.SloganText?.GetTranslation(language),
            Calories = dish.Calories,
            PrepTimeMinutes = dish.PrepTimeMinutes,
            CookTimeMinutes = dish.CookTimeMinutes,
            ImageUrls = dish.DishMedia
                .Where(dm => dm.Media != null)
                .Select(dm => dm.Media!.Url ?? string.Empty)
                .Where(url => !string.IsNullOrEmpty(url))
                .ToList(),
            Composition = dish.Recipes
                .Select(r => new RecipeItemDto
                {
                    IngredientId = r.IngredientId,
                    IngredientName = r.Ingredient?.IngredientNameText?.GetTranslation(language) ?? r.Ingredient?.IngredientName ?? string.Empty,
                    Quantity = r.Quantity,
                    Unit = r.Unit,
                    Note = r.Note
                })
                .ToList()
        };
    }

    /// <inheritdoc />
    public async Task<DishStatusDto> UpdateDishStatusAsync(
        long dishId,
        DishStatusCode newStatus,
        CancellationToken ct = default)
    {
        try
        {
            // Check if dish exists
            var dish = await _dishRepository.FindByIdAsync(dishId, ct);
            if (dish == null)
            {
                _logger.LogWarning("Dish with ID {DishId} not found", dishId);
                throw new KeyNotFoundException($"Dish with ID {dishId} not found.");
            }

            // Resolve status code to lookup value ID using extension method
            var statusId = await newStatus.ToDishStatusIdAsync(_lookupResolver, ct);

            // Update the dish status
            await _dishRepository.UpdateStatusAsync(dishId, statusId, ct);

            _logger.LogInformation(
                "Updated dish {DishId} ({DishName}) status to {Status}",
                dishId,
                dish.DishName,
                newStatus);

            return new DishStatusDto
            {
                DishId = dish.DishId,
                DishName = dish.DishName,
                StatusCode = newStatus,
                StatusId = statusId,
                UpdatedAt = DateTime.UtcNow
            };
        }
        catch (KeyNotFoundException)
        {
            // Re-throw KeyNotFoundException to be handled by controller
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating dish status for dish ID {DishId}", dishId);
            throw new InvalidOperationException($"Failed to update dish status: {ex.Message}", ex);
        }
    }

    public async Task<(List<DishManagementDto> Items, int TotalCount)> GetDishesForAdminAsync(GetDishesRequest request, CancellationToken cancellationToken = default)
    {
        var (entities, totalCount) = await _dishRepository.GetDishesAsync(request, cancellationToken);

        // Chỉ map sang DTO
        var dtos = entities.Select(d => new DishManagementDto
        {
            DishId = d.DishId,
            DishName = d.DishName,
            CategoryName = d.Category?.CategoryName ?? "Uncategorized",
            Price = d.Price,
            Status = d.DishStatusLv?.ValueCode,
            IsOnline = d.IsOnline ?? false,
            CreatedAt = d.CreatedAt
        }).ToList();

        return (dtos, totalCount);
    }

    public async Task<(List<DishDisplayDto> Items, int TotalCount)> GetDishesForCustomerAsync(GetDishesRequest request, CancellationToken cancellationToken = default)
    {
        // Có thể thêm logic filter nghiệp vụ ở đây nếu cần (ví dụ chỉ lấy Active)
        var (entities, totalCount) = await _dishRepository.GetDishesAsync(request, cancellationToken);

        var dtos = entities.Select(d => new DishDisplayDto
        {
            DishId = d.DishId,
            DishName = d.DishName,
            Price = d.Price,
            CategoryName = d.Category?.CategoryName,
            Tagline = d.Slogan ?? d.ShortDescription,
            IsChefRecommended = d.ChefRecommended ?? false,
            ImageUrl = d.DishMedia.FirstOrDefault()?.Media?.Url
        }).ToList();

        return (dtos, totalCount);
    }
}
