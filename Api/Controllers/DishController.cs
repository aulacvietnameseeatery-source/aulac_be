using API.Models;
using Core.DTO.Dish;
using Core.Interface.Repo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>
/// Dish controller providing endpoints for dish operations
/// </summary>
[ApiController]
[Route("api/dishes")]
[Produces("application/json")]
public class DishController : ControllerBase
{
    private readonly IDishRepository _dishRepository;
    private readonly ILogger<DishController> _logger;

    public DishController(
        IDishRepository dishRepository,
        ILogger<DishController> logger)
    {
        _dishRepository = dishRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get dish detail by ID
    /// </summary>
    /// <param name="id">Dish ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dish detail information</returns>
    /// <response code="200">Dish found</response>
    /// <response code="404">Dish not found</response>
    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<DishDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDishById(
        long id,
        CancellationToken cancellationToken)
    {
        var dish = await _dishRepository.GetDishByIdAsync(id, cancellationToken);

        if (dish == null)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Code = 404,
                UserMessage = "Dish not found.",
                Data = null,
                ServerTime = DateTimeOffset.UtcNow
            });
        }

        var dishDto = new DishDetailDto
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

        return Ok(new ApiResponse<DishDetailDto>
        {
            Success = true,
            Code = 200,
            UserMessage = "Dish retrieved successfully.",
            Data = dishDto,
            ServerTime = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Get all dishes
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of dishes</returns>
    /// <response code="200">Dishes retrieved successfully</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<List<DishDetailDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllDishes(CancellationToken cancellationToken)
    {
        var dishes = await _dishRepository.GetAllDishesAsync(cancellationToken);

        var dishDtos = dishes.Select(dish => new DishDetailDto
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

        return Ok(new ApiResponse<List<DishDetailDto>>
        {
            Success = true,
            Code = 200,
            UserMessage = "Dishes retrieved successfully.",
            Data = dishDtos,
            ServerTime = DateTimeOffset.UtcNow
        });
    }
}
