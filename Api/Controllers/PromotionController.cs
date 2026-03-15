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

        [HttpGet]
        //[HasPermission(Permissions.ViewPromotion)]
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

        // _OLD: coupon endpoint moved to CouponController using ICouponService and CouponDTO.
        // [HttpGet("~/api/coupons")]
        // public async Task<IActionResult> GetCoupons(CancellationToken ct)
        // {
        //     var result = await _promotionService.GetCouponListAsync(ct);
        //
        //     return Ok(new ApiResponse<List<PromotionListDTO>>
        //     {
        //         Success = true,
        //         Code = 200,
        //         SubCode = 0,
        //         UserMessage = "Get coupons successfully",
        //         Data = result,
        //         ServerTime = DateTimeOffset.Now
        //     });
        // }

        /// <summary>
        /// Create promotion
        /// </summary>
        [HttpPost]
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
        /// Update promotion
        /// </summary>
        [HttpPut("{promotionId}")]
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

        [HttpGet("{promotionId}")]
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

        [HttpGet("detail/{promotionId}")]
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

        [HttpPut("{promotionId}/disable")]
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

        [HttpPut("{promotionId}/activate")]
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

        [HttpGet("available/{orderId}")]
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
