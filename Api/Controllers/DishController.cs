using API.Models;
using Core.Attribute;
using Core.Data;
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

    // --- ADMIN ENDPOINT ---
    [HttpGet("management")]
    //[HasPermission(Permissions.ViewDish)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<DishManagementDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDishesForAdmin(
        [FromQuery] GetDishesRequest request,
        CancellationToken cancellationToken)
    {
        // 1. Lấy data raw và count từ Service
        var (items, totalCount) = await _dishService.GetDishesForAdminAsync(request, cancellationToken);

        // 2. Tính toán phân trang tại Controller
        var totalPage = request.PageSize > 0
            ? (int)Math.Ceiling((double)totalCount / request.PageSize)
            : 0;

        // 3. Đóng gói PagedResult
        var pagedResult = new PagedResult<DishManagementDto>
        {
            PageData = items,
            PageIndex = request.PageIndex,
            PageSize = request.PageSize,
            TotalCount = totalCount,
            TotalPage = totalPage
        };

        // 4. Trả về ApiResponse
        return Ok(new ApiResponse<PagedResult<DishManagementDto>>
        {
            Success = true,
            Code = 200,
            UserMessage = "Get dish list for admin successfully.",
            Data = pagedResult,
            ServerTime = DateTimeOffset.UtcNow
        });
    }

    // --- CUSTOMER ENDPOINT ---
    [HttpGet("menu")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<DishDisplayDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMenuForCustomer(
        [FromQuery] GetDishesRequest request,
        CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _dishService.GetDishesForCustomerAsync(request, cancellationToken);

        var totalPage = request.PageSize > 0
            ? (int)Math.Ceiling((double)totalCount / request.PageSize)
            : 0;

        var pagedResult = new PagedResult<DishDisplayDto>
        {
            PageData = items,
            PageIndex = request.PageIndex,
            PageSize = request.PageSize,
            TotalCount = totalCount,
            TotalPage = totalPage
        };

        return Ok(new ApiResponse<PagedResult<DishDisplayDto>>
        {
            Success = true,
            Code = 200,
            UserMessage = "Get menu successfully.",
            Data = pagedResult,
            ServerTime = DateTimeOffset.UtcNow
        });
    }
    /// <summary>
    /// Updates the status of a dish.
    /// </summary>
    /// <param name="id">The dish ID</param>
    /// <param name="request">The status update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated dish status information</returns>
    /// <response code="200">Status updated successfully</response>
    /// <response code="404">Dish not found</response>
    /// <response code="400">Invalid request</response>
    /// <remarks>
    /// Updates the dish status to one of the following values:
    /// - AVAILABLE: Dish is available for ordering
    /// - OUT_OF_STOCK: Dish is temporarily out of stock
    /// - HIDDEN: Dish is hidden from the menu
    /// 
    /// The status is stored using the lookup system for internationalization support.
    /// </remarks>
    [HttpPatch("{id}/status")]
    //[HasPermission(Permissions.EditDish)]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<DishStatusDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateDishStatus(
        long id,
        [FromBody] UpdateDishStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _dishService.UpdateDishStatusAsync(id, request.Status, cancellationToken);

        _logger.LogInformation(
            "Dish {DishId} status updated to {Status}",
            id,
            request.Status);

        return Ok(new ApiResponse<DishStatusDto>
        {
            Success = true,
            Code = 200,
            UserMessage = $"Dish status updated to {request.Status} successfully.",
            SystemMessage = "Status update successful",
            Data = result,
            ServerTime = DateTimeOffset.UtcNow
        });
    }
}
