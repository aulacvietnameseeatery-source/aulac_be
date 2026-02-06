using API.Models;
using Core.DTO.Auth;
using Core.DTO.Dish;
using Core.Interface.Service.Dish;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Api.Controllers
{
    [Route("api/dishes")]
    [ApiController]
    public class DishController : Controller
    {
        private readonly IDishService _dishService;
        private readonly ILogger<DishController> _logger;
        public DishController(IDishService dishService, ILogger<DishController> logger)
        {
            _dishService = dishService;
            _logger = logger;
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
        [HttpGet("{id}")]
        public async Task<ActionResult<DishDetailDto>> GetById(long id, CancellationToken ct)
        {
            var result = await _dishService.GetDishByIdAsync(id, ct);
            return Ok(new ApiResponse<DishDetailDto>
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
            var result = await _dishService.GetActiveDishStatusesAsync();
            //return Ok(result);
            return Ok(new ApiResponse<List<ActiveDishStatusDto>>
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
        [HttpGet("categories")]
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
            var result = await _dishService.GetAllActiveTagsAsync();
            return Ok(new ApiResponse<List<DishTagDto>>
            {
                Success = true,
                Code = 200,
                UserMessage = $"Get Active Dish Tags",
                Data = result,
                ServerTime = DateTimeOffset.UtcNow
            });
        }
    }
}
