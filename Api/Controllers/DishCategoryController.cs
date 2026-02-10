using API.Models;
using Core.Attribute;
using Core.Data;
using Core.DTO.DishCategory;
using Core.Interface.Service.Entity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>
/// Dish category controller providing endpoints for category management operations
/// </summary>
[ApiController]
[Route("api/dish-categories")]
public class DishCategoryController : ControllerBase
{
    private readonly IDishCategoryService _dishCategoryService;
    private readonly ILogger<DishCategoryController> _logger;

    public DishCategoryController(
        IDishCategoryService dishCategoryService,
        ILogger<DishCategoryController> logger)
    {
        _dishCategoryService = dishCategoryService;
        _logger = logger;
    }

    /// <summary>
    /// Get all dish categories
    /// </summary>
    /// <param name="includeDisabled">Whether to include disabled categories</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of dish categories</returns>
    /// <response code="200">Categories retrieved successfully</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<List<DishCategoryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllCategories(
        [FromQuery] bool includeDisabled = false,
        CancellationToken cancellationToken = default)
    {
        var categories = await _dishCategoryService.GetAllCategoriesAsync(includeDisabled, cancellationToken);

        return Ok(new ApiResponse<List<DishCategoryDto>>
        {
            Success = true,
            Code = 200,
            UserMessage = "Categories retrieved successfully.",
            Data = categories,
            ServerTime = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Get dish category by ID
    /// </summary>
    /// <param name="id">Category ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dish category detail</returns>
    /// <response code="200">Category found</response>
    /// <response code="404">Category not found</response>
    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<DishCategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategoryById(
        long id,
        CancellationToken cancellationToken = default)
    {
        var category = await _dishCategoryService.GetCategoryByIdAsync(id, cancellationToken);

        if (category == null)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Code = 404,
                UserMessage = "Category not found.",
                Data = default!,
                ServerTime = DateTimeOffset.UtcNow
            });
        }

        return Ok(new ApiResponse<DishCategoryDto>
        {
            Success = true,
            Code = 200,
            UserMessage = "Category retrieved successfully.",
            Data = category,
            ServerTime = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Create a new dish category
    /// </summary>
    /// <param name="request">Create category request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created category</returns>
    /// <response code="201">Category created successfully</response>
    /// <response code="400">Invalid request</response>
    /// <response code="409">Category name already exists</response>
    [HttpPost]
    [HasPermission(Permissions.CreateDishCategory)]
    [ProducesResponseType(typeof(ApiResponse<DishCategoryDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateCategory(
        [FromBody] CreateDishCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Code = 400,
                UserMessage = "Invalid request data.",
                Data = ModelState,
                ServerTime = DateTimeOffset.UtcNow
            });
        }

        try
        {
            var category = await _dishCategoryService.CreateCategoryAsync(request, cancellationToken);

            _logger.LogInformation("Created new category: {CategoryName} (ID: {CategoryId})", 
                category.CategoryName, category.CategoryId);

            return CreatedAtAction(
                nameof(GetCategoryById),
                new { id = category.CategoryId },
                new ApiResponse<DishCategoryDto>
                {
                    Success = true,
                    Code = 201,
                    UserMessage = "Category created successfully.",
                    Data = category,
                    ServerTime = DateTimeOffset.UtcNow
                });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ApiResponse<object>
            {
                Success = false,
                Code = 409,
                UserMessage = ex.Message,
                Data = default!,
                ServerTime = DateTimeOffset.UtcNow
            });
        }
    }

    /// <summary>
    /// Update an existing dish category
    /// </summary>
    /// <param name="id">Category ID</param>
    /// <param name="request">Update category request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated category</returns>
    /// <response code="200">Category updated successfully</response>
    /// <response code="400">Invalid request</response>
    /// <response code="404">Category not found</response>
    /// <response code="409">Category name already exists</response>
    [HttpPut("{id}")]
    [HasPermission(Permissions.EditDishCategory)]
    [ProducesResponseType(typeof(ApiResponse<DishCategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateCategory(
        long id,
        [FromBody] UpdateDishCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Code = 400,
                UserMessage = "Invalid request data.",
                Data = ModelState,
                ServerTime = DateTimeOffset.UtcNow
            });
        }

        try
        {
            var category = await _dishCategoryService.UpdateCategoryAsync(id, request, cancellationToken);

            _logger.LogInformation("Updated category: {CategoryName} (ID: {CategoryId})", 
                category.CategoryName, category.CategoryId);

            return Ok(new ApiResponse<DishCategoryDto>
            {
                Success = true,
                Code = 200,
                UserMessage = "Category updated successfully.",
                Data = category,
                ServerTime = DateTimeOffset.UtcNow
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Code = 404,
                UserMessage = ex.Message,
                Data = default!,
                ServerTime = DateTimeOffset.UtcNow
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ApiResponse<object>
            {
                Success = false,
                Code = 409,
                UserMessage = ex.Message,
                Data = default!,
                ServerTime = DateTimeOffset.UtcNow
            });
        }
    }

    /// <summary>
    /// Toggle category status (enable/disable)
    /// </summary>
    /// <param name="id">Category ID</param>
    /// <param name="isDisabled">New disabled status</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated category</returns>
    /// <response code="200">Status toggled successfully</response>
    /// <response code="404">Category not found</response>
    [HttpPatch("{id}/status")]
    [HasPermission(Permissions.EditDishCategory)]
    [ProducesResponseType(typeof(ApiResponse<DishCategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleCategoryStatus(
        long id,
        [FromBody] bool isDisabled,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var category = await _dishCategoryService.ToggleCategoryStatusAsync(id, isDisabled, cancellationToken);

            _logger.LogInformation("Toggled category status: {CategoryName} (ID: {CategoryId}) - IsDisabled: {IsDisabled}", 
                category.CategoryName, category.CategoryId, isDisabled);

            return Ok(new ApiResponse<DishCategoryDto>
            {
                Success = true,
                Code = 200,
                UserMessage = $"Category {(isDisabled ? "disabled" : "enabled")} successfully.",
                Data = category,
                ServerTime = DateTimeOffset.UtcNow
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Code = 404,
                UserMessage = ex.Message,
                Data = default!,
                ServerTime = DateTimeOffset.UtcNow
            });
        }
    }

}
