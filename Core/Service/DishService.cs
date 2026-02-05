using Core.DTO.Dish;
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
    public async Task<DishDetailDto?> GetDishByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var dish = await _dishRepository.GetDishByIdAsync(id, cancellationToken);

        if (dish == null)
        {
            return null;
        }

        return new DishDetailDto
        {
            DishId = dish.DishId,
            DishName = dish.DishName,
            Price = dish.Price,
            CategoryName = dish.Category.CategoryName,
            Description = dish.Description,
            ShortDescription = dish.ShortDescription,
            Slogan = dish.Slogan,
            Calories = dish.Calories,
            PrepTimeMinutes = dish.PrepTimeMinutes,
            CookTimeMinutes = dish.CookTimeMinutes,
            ImageUrls = dish.DishMedia
                .Where(dm => dm.Media != null)
                .Select(dm => dm.Media!.Url ?? string.Empty)
                .Where(url => !string.IsNullOrEmpty(url))
                .ToList()
        };
    }

    /// <inheritdoc/>
    public async Task<List<DishDetailDto>> GetAllDishesAsync(CancellationToken cancellationToken = default)
    {
        var dishes = await _dishRepository.GetAllDishesAsync(cancellationToken);

        return dishes.Select(dish => new DishDetailDto
        {
            DishId = dish.DishId,
            DishName = dish.DishName,
            Price = dish.Price,
            CategoryName = dish.Category.CategoryName,
            Description = dish.Description,
            ShortDescription = dish.ShortDescription,
            Slogan = dish.Slogan,
            Calories = dish.Calories,
            PrepTimeMinutes = dish.PrepTimeMinutes,
            CookTimeMinutes = dish.CookTimeMinutes,
            ImageUrls = dish.DishMedia
                .Where(dm => dm.Media != null)
                .Select(dm => dm.Media!.Url ?? string.Empty)
                .Where(url => !string.IsNullOrEmpty(url))
                .ToList()
        }).ToList();
    }
}
