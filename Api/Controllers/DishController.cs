using API.Models;
using Core.Attribute;
using Core.Data;
using Core.DTO.Dish;
using Core.Interface.Service.Entity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Route("api/dishes")]
[ApiController]
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
