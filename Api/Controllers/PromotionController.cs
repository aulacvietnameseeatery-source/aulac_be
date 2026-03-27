using API.Models;
using Core.Attribute;
using Core.Data;
using Core.DTO.General;
using Core.DTO.Promotion;
using Core.Interface.Service.Promotion;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [Route("api/promotions")]
    [ApiController]
    public class PromotionController : ControllerBase
    {
        private readonly IPromotionService _promotionService;
        private readonly ILogger<PromotionController> _logger;

        public PromotionController(
            IPromotionService promotionService,
            ILogger<PromotionController> logger)
        {
            _promotionService = promotionService;
            _logger = logger;
        }

        /// <summary>
        /// Get list of promotions with filter and paging
        /// </summary>
        /// <remarks>
        /// Requires permission: ViewPromotion
        /// </remarks>
        [HttpGet]
        [HasPermission(Permissions.ViewPromotion)]
        public async Task<IActionResult> GetPromotions(
            [FromQuery] PromotionListQueryDTO query,
            CancellationToken ct)
        {
            var result = await _promotionService.GetPromotionsAsync(query, ct);

            return Ok(new ApiResponse<PagedResultDTO<PromotionListDTO>>
            {
                Success = true,
                Code = 200,
                SubCode = 0,
                UserMessage = "Get promotions successfully",
                Data = result,
                ServerTime = DateTimeOffset.Now
            });
        }

        /// <summary>
        /// Create a new promotion
        /// </summary>
        /// <remarks>
        /// Requires permission: CreatePromotion
        /// </remarks>
        [HttpPost]
        [HasPermission(Permissions.CreatePromotion)]
        public async Task<IActionResult> CreatePromotion(
            [FromBody] PromotionDto request,
            CancellationToken ct)
        {
            var id = await _promotionService.CreatePromotionAsync(request, ct);

            return Ok(new ApiResponse<long>
            {
                Success = true,
                Code = 200,
                Data = id,
                UserMessage = "Promotion created successfully",
                ServerTime = DateTimeOffset.Now
            });
        }

        /// <summary>
        /// Update an existing promotion
        /// </summary>
        /// <remarks>
        /// Requires permission: UpdatePromotion
        /// </remarks>
        [HttpPut("{promotionId}")]
        [HasPermission(Permissions.UpdatePromotion)]
        public async Task<IActionResult> UpdatePromotion(
            long promotionId,
            [FromBody] PromotionDto request,
            CancellationToken ct)
        {
            request.PromotionId = promotionId;

            await _promotionService.UpdatePromotionAsync(request, ct);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Code = 200,
                UserMessage = "Promotion updated successfully",
                ServerTime = DateTimeOffset.Now
            });
        }

        /// <summary>
        /// Get promotion by id
        /// </summary>
        /// <remarks>
        /// Requires permission: ViewPromotion
        /// </remarks>
        [HttpGet("{promotionId}")]
        [HasPermission(Permissions.ViewPromotion)]
        [ProducesResponseType(typeof(ApiResponse<PromotionDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPromotionById(
            long promotionId,
            CancellationToken ct)
        {
            var result = await _promotionService
                .GetPromotionByIdAsync(promotionId, ct);

            return Ok(new ApiResponse<PromotionDto>
            {
                Success = true,
                Code = 200,
                UserMessage = "Get promotion successfully",
                Data = result,
                ServerTime = DateTimeOffset.Now
            });
        }

        /// <summary>
        /// Get promotion detail by id
        /// </summary>
        /// <remarks>
        /// Requires permission: ViewPromotion
        /// </remarks>
        [HttpGet("detail/{promotionId}")]
        [HasPermission(Permissions.ViewPromotion)]
        public async Task<IActionResult> GetPromotionDetail(
            long promotionId,
            CancellationToken ct)
        {
            var result = await _promotionService
                .GetPromotionDetailAsync(promotionId, ct);

            return Ok(new ApiResponse<PromotionDetailDto>
            {
                Success = true,
                Code = 200,
                Data = result,
                ServerTime = DateTimeOffset.Now
            });
        }

        /// <summary>
        /// Disable a promotion
        /// </summary>
        /// <remarks>
        /// Requires permission: UpdatePromotion
        /// </remarks>
        [HttpPut("{promotionId}/disable")]
        [HasPermission(Permissions.UpdatePromotion)]
        public async Task<IActionResult> DisablePromotion(
            long promotionId,
            CancellationToken ct)
        {
            await _promotionService.DisablePromotionAsync(promotionId, ct);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Code = 200,
                UserMessage = "Promotion disabled successfully",
                ServerTime = DateTimeOffset.Now
            });
        }

        /// <summary>
        /// Activate a promotion
        /// </summary>
        /// <remarks>
        /// Requires permission: UpdatePromotion
        /// </remarks>
        [HttpPut("{promotionId}/activate")]
        [HasPermission(Permissions.UpdatePromotion)]
        public async Task<IActionResult> ActivatePromotion(
            long promotionId,
            CancellationToken ct)
        {
            await _promotionService.ActivatePromotionAsync(promotionId, ct);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Code = 200,
                UserMessage = "Promotion activated successfully",
                ServerTime = DateTimeOffset.Now
            });
        }

        /// <summary>
        /// Get available promotions for an order
        /// </summary>
        /// <remarks>
        /// Requires permission: ViewPromotion
        /// </remarks>
        [HttpGet("available/{orderId}")]
        [HasPermission(Permissions.ViewPromotion)]
        public async Task<IActionResult> GetAvailablePromotions(
            long orderId,
            CancellationToken ct)
        {
            var result = await _promotionService
                .GetAvailablePromotionsAsync(orderId, ct);

            return Ok(new ApiResponse<List<PromotionAvailableDTO>>
            {
                Success = true,
                Code = 200,
                SubCode = 0,
                UserMessage = "Get available promotions successfully",
                Data = result,
                ServerTime = DateTimeOffset.Now
            });
        }
    }
}
