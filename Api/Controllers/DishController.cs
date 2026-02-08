using API.Models;
using Core.DTO.Dish;
using Core.Entity;
using Core.Interface.Service.Entity;
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
    private readonly IDishService _dishService;
    private readonly ILogger<DishController> _logger;

    public DishController(
        IDishService dishService,
        ILogger<DishController> logger)
    {
        _dishService = dishService;
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
        var dishDto = await _dishService.GetDishByIdAsync(id, cancellationToken);

        if (dishDto == null)
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

        return Ok(new ApiResponse<DishDetailDto>
        {
            Success = true,
            Code = 200,
            UserMessage = "Dish retrieved successfully.",
            Data = dishDto,
            ServerTime = DateTimeOffset.UtcNow
        });
    }

    public async Task<IActionResult> GetDishes(
        [FromQuery] GetDishesRequest request,
        CancellationToken cancellationToken)
    {
        // 1. Get data from service (Pass cancellationToken down)
        var (items, totalCount) = await _dishService.GetAllDishesAsync(request, cancellationToken);

        // 2. Calculate total pages
        // Optimization: Handle potential DivideByZero if PageSize is 0 (though DTO validation usually handles this)
        var totalPage = request.PageSize > 0
            ? (int)Math.Ceiling((double)totalCount / request.PageSize)
            : 0;

        // 3. Encapsulate data into PagedResult
        var pagedResult = new PagedResult<Dish>
        {
            PageData = items,
            PageIndex = request.PageIndex,
            PageSize = request.PageSize,
            TotalCount = totalCount,
            TotalPage = totalPage
        };

        // 4. Return standard ApiResponse
        return Ok(new ApiResponse<PagedResult<Dish>>
        {
            Success = true,
            Code = 200,
            UserMessage = "Get dish list successfully.",
            Data = pagedResult,
            ServerTime = DateTimeOffset.UtcNow
        });
    }
}
