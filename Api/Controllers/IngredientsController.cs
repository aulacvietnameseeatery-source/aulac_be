using API.Models;
using Core.DTO.Ingredient;
using Core.Interface.Service;
using Core.Interface.Service.Entity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>
/// Ingredient controller providing endpoints for ingredient management and stock operations
/// </summary>
[ApiController]
[Route("api/ingredients")]
public class IngredientsController : ControllerBase
{
    private readonly IIngredientService _ingredientService;
    private readonly ILogger<IngredientsController> _logger;

    public IngredientsController(
        IIngredientService ingredientService,
        ILogger<IngredientsController> logger)
    {
        _ingredientService = ingredientService;
        _logger = logger;
    }

    // --- 1. INGREDIENT MANAGEMENT ---

    /// <summary>
    /// Get paginated list of ingredients with optional filters
    /// </summary>
    /// <param name="filter">Filter parameters (Search, Type, LowStock)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of ingredients</returns>
    [HttpGet]
    // [HasPermission(Permissions.ViewIngredient)] // Mở ra khi có Auth
    [ProducesResponseType(typeof(ApiResponse<PagedResult<IngredientDTO>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetIngredients(
        [FromQuery] IngredientFilterParams filter,
        CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _ingredientService.GetListAsync(filter);

        var pagedResult = new PagedResult<IngredientDTO>
        {
            PageData = items,
            PageIndex = filter.PageIndex,
            PageSize = filter.PageSize,
            TotalCount = totalCount,
            TotalPage = filter.PageSize > 0
                ? (int)Math.Ceiling((double)totalCount / filter.PageSize)
                : 0
        };

        return Ok(new ApiResponse<PagedResult<IngredientDTO>>
        {
            Success = true,
            Code = 200,
            UserMessage = "Ingredients retrieved successfully.",
            Data = pagedResult,
            ServerTime = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Get all ingredients (simple list for dropdowns)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Simple list of all ingredients</returns>
    [HttpGet("all")]
    [ProducesResponseType(typeof(ApiResponse<List<IngredientSimpleDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllIngredients(CancellationToken cancellationToken)
    {
        var filter = new IngredientFilterParams 
        { 
            PageIndex = 1, 
            PageSize = int.MaxValue 
        };
        
        var (items, _) = await _ingredientService.GetListAsync(filter);
        
        var simpleList = items.Select(i => new IngredientSimpleDto
        {
            IngredientId = i.IngredientId,
            IngredientName = i.IngredientName,
            Unit = i.Unit
        }).ToList();

        return Ok(new ApiResponse<List<IngredientSimpleDto>>
        {
            Success = true,
            Code = 200,
            UserMessage = "All ingredients retrieved successfully.",
            Data = simpleList,
            ServerTime = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Get ingredient detail by ID
    /// </summary>
    /// <param name="id">Ingredient ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Ingredient detail information</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<IngredientDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetIngredientById(
        long id,
        CancellationToken cancellationToken)
    {
        var result = await _ingredientService.GetDetailAsync(id);

        return Ok(new ApiResponse<IngredientDTO>
        {
            Success = true,
            Code = 200,
            UserMessage = "Ingredient detail retrieved successfully.",
            Data = result,
            ServerTime = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Create a new ingredient
    /// </summary>
    /// <param name="request">Ingredient data payload</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created ingredient data</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<IngredientDTO>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateIngredient(
        [FromBody] SaveIngredientRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _ingredientService.CreateAsync(request);

        _logger.LogInformation("Created new ingredient: {IngredientName}", request.IngredientName);

        return Ok(new ApiResponse<IngredientDTO>
        {
            Success = true,
            Code = 200,
            UserMessage = "Ingredient created successfully.",
            Data = result,
            ServerTime = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Update an existing ingredient
    /// </summary>
    /// <param name="id">Ingredient ID</param>
    /// <param name="request">Ingredient data payload</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated ingredient data</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<IngredientDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateIngredient(
        long id,
        [FromBody] SaveIngredientRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _ingredientService.UpdateAsync(id, request);

        _logger.LogInformation("Updated ingredient ID: {IngredientId}", id);

        return Ok(new ApiResponse<IngredientDTO>
        {
            Success = true,
            Code = 200,
            UserMessage = "Ingredient updated successfully.",
            Data = result,
            ServerTime = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Delete an ingredient
    /// </summary>
    /// <param name="id">Ingredient ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteIngredient(
        long id,
        CancellationToken cancellationToken)
    {
        await _ingredientService.DeleteAsync(id);

        _logger.LogInformation("Deleted ingredient ID: {IngredientId}", id);

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Code = 200,
            UserMessage = "Ingredient deleted successfully.",
            Data = null!,
            ServerTime = DateTimeOffset.UtcNow
        });
    }

    // --- 2. STOCK OPERATIONS ---

    /// <summary>
    /// Adjust stock quantity for an ingredient (Manual Import/Export)
    /// </summary>
    /// <param name="id">Ingredient ID</param>
    /// <param name="request">Adjustment details (Positive for import, Negative for export)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    [HttpPost("{id}/adjust-stock")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> AdjustStock(
        long id,
        [FromBody] AdjustStockRequest request,
        CancellationToken cancellationToken)
    {
        await _ingredientService.AdjustStockAsync(id, request);

        _logger.LogInformation("Adjusted stock for ingredient {IngredientId}. Quantity changed: {Qty}", id, request.Quantity);

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Code = 200,
            UserMessage = "Stock adjusted successfully.",
            Data = null!,
            ServerTime = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Get the history of stock transactions for a specific ingredient
    /// </summary>
    /// <param name="id">Ingredient ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of stock transaction history</returns>
    [HttpGet("{id}/stock-history")]
    [ProducesResponseType(typeof(ApiResponse<List<StockHistoryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStockHistory(
        long id,
        CancellationToken cancellationToken)
    {
        var result = await _ingredientService.GetStockHistoryAsync(id);

        return Ok(new ApiResponse<List<StockHistoryDto>>
        {
            Success = true,
            Code = 200,
            UserMessage = "Stock history retrieved successfully.",
            Data = result,
            ServerTime = DateTimeOffset.UtcNow
        });
    }
}