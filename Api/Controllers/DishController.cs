using API.Models;
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

    }
}
