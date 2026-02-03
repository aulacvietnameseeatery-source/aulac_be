using API.Models;
using Core.DTO.Dish;
using Core.Entity;
using Core.Interface.Service.Dishes;
using Core.Service;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DishControllers : ControllerBase
    {
        private readonly IDishService dishService;
        public DishControllers(IDishService dishService)
        {
            this.dishService = dishService;
        }

        [HttpGet]
        public async Task<IActionResult> GetDishes([FromQuery] GetDishesRequest request)
        {
            // get data dish list and total count from service
            var (items, totalCount) = await dishService.GetAllDishesAsync(request);

            // calculate total page
            // Ceil(total / total per page)
            var totalPage = (int)Math.Ceiling((double)totalCount / request.PageSize);

            // encapsulate it for pagedResult (Data Model)
            var pagedResult = new PagedResult<Dish>
            {
                PageData = items,
                PageIndex = request.PageIndex,
                PageSize = request.PageSize,
                TotalCount = totalCount,
                TotalPage = totalPage
            };

            // encapsulate it for ApiResponse (Response Model)
            var response = new ApiResponse<PagedResult<Dish>>
            {
                Success = true,
                Code = 200,
                Data = pagedResult,
                UserMessage = "Get Dish List Successfully",
                ServerTime = DateTimeOffset.UtcNow
            };

            return Ok(response);
        }

    }
}
