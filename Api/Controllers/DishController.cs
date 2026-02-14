using API.Models;
using Core.Attribute;
using Core.Data;
using Core.DTO.Auth;
using Core.DTO.Dish;
using Core.DTO.LookUpValue;
using Core.Entity;
using Core.Interface.Service.Entity;
using Core.Interface.Service.LookUp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

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
    private readonly ILookupService _lookupService;

    public DishController(
        IDishService dishService,
        ILogger<DishController> logger,
        ILookupService lookupService)
    {
        _dishService = dishService;
        _logger = logger;
        _lookupService = lookupService;
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

    // --- 1. ADMIN ENDPOINT (Quản lý món ăn) ---
    [HttpGet("management")]
    // [HasPermission(Permissions.ViewDish)] // Bật lại khi có Auth
    [ProducesResponseType(typeof(ApiResponse<PagedResult<DishManagementDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDishesForAdmin(
        [FromQuery] GetDishesRequest request,
        CancellationToken cancellationToken)
    {
        // Gọi Service
        var (items, totalCount) = await _dishService.GetDishesForAdminAsync(request, cancellationToken);

        // Đóng gói PagedResult (Nên tách ra helper để tái sử dụng)
        var pagedResult = new PagedResult<DishManagementDto>
        {
            PageData = items,
            PageIndex = request.PageIndex,
            PageSize = request.PageSize,
            TotalCount = totalCount,
            TotalPage = request.PageSize > 0
                ? (int)Math.Ceiling((double)totalCount / request.PageSize)
                : 0
        };

        return Ok(new ApiResponse<PagedResult<DishManagementDto>>
        {
            Success = true,
            Code = 200,
            UserMessage = "Dishes retrieved successfully.", // Message nên tổng quát
            Data = pagedResult,
            ServerTime = DateTimeOffset.UtcNow
        });
    }

    // --- 2. CUSTOMER ENDPOINT (Menu hiển thị) ---
    [HttpGet("menu")]
    [AllowAnonymous] // Khách không cần login
    [ProducesResponseType(typeof(ApiResponse<PagedResult<DishDisplayDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMenuForCustomer(
        [FromQuery] GetDishesRequest request,
        CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _dishService.GetDishesForCustomerAsync(request, cancellationToken);

        var pagedResult = new PagedResult<DishDisplayDto>
        {
            PageData = items,
            PageIndex = request.PageIndex,
            PageSize = request.PageSize,
            TotalCount = totalCount,
            TotalPage = request.PageSize > 0
                ? (int)Math.Ceiling((double)totalCount / request.PageSize)
                : 0
        };

        return Ok(new ApiResponse<PagedResult<DishDisplayDto>>
        {
            Success = true,
            Code = 200,
            UserMessage = "Menu retrieved successfully.",
            Data = pagedResult,
            ServerTime = DateTimeOffset.UtcNow
        });
    }

    // --- 3. FILTER DROPDOWNS (Dữ liệu cho Frontend Dropdown) ---

    [HttpGet("categories")]
    [AllowAnonymous] // Hoặc authorize tùy nghiệp vụ
    [ProducesResponseType(typeof(ApiResponse<List<string>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllCategories(CancellationToken cancellationToken)
    {
        var categories = await _dishService.GetAllCategoriesAsync(cancellationToken);

        return Ok(new ApiResponse<List<string>>
        {
            Success = true,
            Code = 200,
            Data = categories,
            ServerTime = DateTimeOffset.UtcNow
        });
    }

    [HttpGet("statuses")]
    // [HasPermission(Permissions.ViewDish)] // Chỉ Admin mới cần lọc status
    [ProducesResponseType(typeof(ApiResponse<List<DishStatusDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDishStatuses(CancellationToken cancellationToken)
    {
        // Lưu ý: DTO trả về ở đây phụ thuộc vào cái bạn chọn ở bước trước (DishStatusDto hoặc DishStatusFilterDto)
        var statuses = await _dishService.GetDishStatusesAsync(cancellationToken);

        return Ok(new ApiResponse<List<DishStatusDto>> // Hoặc List<DishStatusFilterDto>
        {
            Success = true,
            Code = 200,
            Data = statuses,
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


    /// <summary>
    /// Creates a new dish with associated media files.
    /// </summary>
    /// <param name="dto">Form data containing dish info and media files.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Returns the ID of the created dish.</returns>
    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CreateDish(
        [FromForm] CreateDishFormRequest dto,
        CancellationToken ct)
    {
        var request = JsonSerializer.Deserialize<CreateDishRequest>(
            dto.Dish,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        )!;

        var result = await _dishService.CreateDishAsync(
            request,
            dto.StaticImages,
            dto.Images360,
            ct
        );

        return Ok(new ApiResponse<long>
        {
            Success = true,
            Data = result
        });
    }

    /// <summary>
    /// Gets the details of a dish by its ID.
    /// </summary>
    /// <param name="id">Dish ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Returns the dish details.</returns>
    [HttpGet("detail/{id}")]
    public async Task<ActionResult<DishDetailForActionsDto>> GetById(long id, CancellationToken ct)
    {
        var result = await _dishService.GetDishByIdAsync(id, ct);
        return Ok(new ApiResponse<DishDetailForActionsDto>
        {
            Success = true,
            Code = 200,
            UserMessage = $"Get Dish Detail ID: {id}",
            Data = result,
            ServerTime = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Updates an existing dish and its media files.
    /// </summary>
    /// <param name="id">Dish ID.</param>
    /// <param name="dto">Form data containing updated dish info and media files.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Returns success message.</returns>
    [HttpPut("{id:long}/edit")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UpdateDishMedia(
        long id,
        [FromForm] UpdateDishFormRequest dto,
        CancellationToken ct
    )
    {
        var request = JsonSerializer.Deserialize<UpdateDishRequest>(
            dto.Dish,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        )!;

        var removedMediaIds = JsonSerializer.Deserialize<List<long>>(
            dto.RemovedMediaIds
        ) ?? new();

        await _dishService.UpdateDishAsync(
            request,
            dto.StaticImages,
            dto.Images360,
            removedMediaIds,
            ct
        );

        return Ok(new ApiResponse<string>
        {
            Success = true,
            Code = 200,
            UserMessage = $"Update Dish Successfully",
            Data = "",
            ServerTime = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Gets all active dish statuses.
    /// </summary>
    /// <returns>Returns a list of active dish statuses.</returns>
    [HttpGet("status/active")]
    public async Task<IActionResult> GetActiveDishStatuses()
    {
        var result = await _lookupService.GetAllActiveByTypeAsync(
                (ushort)Core.Enum.LookupType.DishStatus,
                CancellationToken.None
            );
        return Ok(new ApiResponse<List<LookupValueI18nDto>>
        {
            Success = true,
            Code = 200,
            UserMessage = $"Get Active Dish Status",
            Data = result,
            ServerTime = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Gets all dish categories.
    /// </summary>
    /// <returns>Returns a list of dish categories.</returns>
    [HttpGet("all-categories")]
    public async Task<IActionResult> GetDishCategories()
    {
        var result = await _dishService.GetAllDishCategoriesAsync();
        return Ok(new ApiResponse<List<DishCategorySimpleDto>>
        {
            Success = true,
            Code = 200,
            UserMessage = $"Get Active Dish Categories",
            Data = result,
            ServerTime = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Gets all active dish tags.
    /// </summary>
    /// <returns>Returns a list of active dish tags.</returns>
    [HttpGet("tags")]
    public async Task<IActionResult> GetAllTags()
    {
        var result = await _lookupService.GetAllActiveByTypeAsync(
                (ushort)Core.Enum.LookupType.Tag,
                CancellationToken.None
            );
        return Ok(new ApiResponse<List<LookupValueI18nDto>>
        {
            Success = true,
            Code = 200,
            UserMessage = $"Get Active Dish Tags",
            Data = result,
            ServerTime = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Gets all active dish diet.
    /// </summary>
    /// <returns>Returns a list of active dish tags.</returns>
    [HttpGet("diets")]
    public async Task<IActionResult> GetAllDishDiet()
    {
        var result = await _lookupService.GetAllActiveByTypeAsync(
                (ushort)Core.Enum.LookupType.DishDiet,
                CancellationToken.None
            );
        return Ok(new ApiResponse<List<LookupValueI18nDto>>
        {
            Success = true,
            Code = 200,
            UserMessage = $"Get Active Dish Diets",
            Data = result,
            ServerTime = DateTimeOffset.UtcNow
        });
    }
}
