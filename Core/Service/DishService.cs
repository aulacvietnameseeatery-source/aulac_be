using Core.DTO.Dish;
using Core.Extensions;
using Core.Interface.Repo;
using Core.Interface.Service.Entity;

namespace Core.Service;

/// <summary>
/// Implementation of IDishService handling dish business logic
/// </summary>
public class DishService : IDishService
{
    private readonly IDishRepository _dishRepository;

    public DishService(IDishRepository dishRepository)
    {
        _dishRepository = dishRepository;
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
}
