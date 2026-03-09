using API.Models;
using Core.Data;
using Core.DTO.Ingredient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

/// <summary>
/// Ingredient controller providing endpoints for ingredient operations
/// </summary>
[ApiController]
[Route("api/ingredients")]
public class IngredientController : ControllerBase
{
    private readonly Infa.Data.RestaurantMgmtContext _context;
    private readonly ILogger<IngredientController> _logger;

    public IngredientController(
        Infa.Data.RestaurantMgmtContext context,
        ILogger<IngredientController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all ingredients (simple list for dropdowns)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of ingredients</returns>
    /// <response code="200">Ingredients retrieved successfully</response>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<List<IngredientDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllIngredients(CancellationToken cancellationToken = default)
    {
        var ingredients = await _context.Ingredients
            .OrderBy(i => i.IngredientName)
            .Select(i => new IngredientDto
            {
                IngredientId = i.IngredientId,
                IngredientName = i.IngredientName,
                Unit = i.Unit
            })
            .ToListAsync(cancellationToken);

        return Ok(new ApiResponse<List<IngredientDto>>
        {
            Success = true,
            Code = 200,
            SubCode = 0,
            UserMessage = "Get ingredients successfully",
            Data = ingredients,
            ServerTime = DateTimeOffset.Now
        });
    }
}
