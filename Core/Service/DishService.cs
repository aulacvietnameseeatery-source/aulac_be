
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

    /// <summary>
    /// Lấy danh sách cho Admin (Map sang DishManagementDto)
    /// </summary>
    public async Task<(List<DishManagementDto> Items, int TotalCount)> GetDishesForAdminAsync(
        GetDishesRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Gọi Repository để lấy dữ liệu
            var (entities, totalCount) = await _dishRepository.GetDishesAsync(request, cancellationToken);

            // 2. Map sang DTO của bạn
            var dtos = entities.Select(d => new DishManagementDto
            {
                DishId = d.DishId,
                DishName = d.DishName,

                // Xử lý null cho Category
                CategoryName = d.Category?.CategoryName ?? "Uncategorized",

                Price = d.Price,

                // Map Status (Tên hiển thị)
                Status = d.DishStatusLv?.ValueName ?? "Unknown",

                // Map StatusId (Quan trọng để Frontend tô màu Badge)
                StatusId = d.DishStatusLvId,

                IsOnline = d.IsOnline ?? false,
                CreatedAt = d.CreatedAt
            }).ToList();

            return (dtos, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dishes for admin");
            throw;
        }
    }

    /// <summary>
    /// Lấy danh sách cho Customer (Map sang DishDisplayDto - dùng DTO khác đơn giản hơn)
    /// </summary>
    public async Task<(List<DishDisplayDto> Items, int TotalCount)> GetDishesForCustomerAsync(
        GetDishesRequest request,
        CancellationToken cancellationToken = default)
    {
        // ... (Giữ nguyên logic customer nếu bạn có DishDisplayDto, nếu không thì dùng chung DTO trên cũng được) ...
        // Logic mẫu:
        request.IsCustomerView = true;
        var (entities, totalCount) = await _dishRepository.GetDishesAsync(request, cancellationToken);

        var dtos = entities.Select(d => new DishDisplayDto
        {
            DishId = d.DishId,
            DishName = d.DishName,
            Price = d.Price,
            CategoryName = d.Category?.CategoryName,
            ImageUrl = d.DishMedia.FirstOrDefault()?.Media?.Url,
            // ... các trường khác
        }).ToList();

        return (dtos, totalCount);
    }

    // --- CÁC HÀM HỖ TRỢ DROPDOWN (Giữ nguyên) ---

    public async Task<List<string>> GetAllCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await _dishRepository.GetAllCategoriesAsync(cancellationToken);
    }

    public async Task<List<DishStatusDto>> GetDishStatusesAsync(CancellationToken cancellationToken = default)
    {
        var statuses = await _dishRepository.GetDishStatusesAsync(cancellationToken);

        return statuses.Select(s => new DishStatusDto
        {
            StatusId = s.ValueId,
            StatusName = s.ValueName
        }).ToList();
    }
}

