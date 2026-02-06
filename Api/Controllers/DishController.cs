using API.Models;
using Core.DTO.Dish;
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
    /// <param name="lang">Language code. Supported: "en" (English - default), "fr" (French), "vi" (Vietnamese)</param>
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
        [FromQuery] string? lang,
        CancellationToken cancellationToken)
    {
        var dishDto = await _dishService.GetDishByIdAsync(id, lang, cancellationToken);

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
}
