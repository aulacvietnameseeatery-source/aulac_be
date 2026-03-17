using API.Models;
using Core.DTO.Coupon;
using Core.Interface.Service.Coupon;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [Route("api/coupons")]
    [ApiController]
    public class CouponController : ControllerBase
    {
        private readonly ICouponService _couponService;

        public CouponController(ICouponService couponService)
        {
            _couponService = couponService;
        }

        [HttpGet]
        public async Task<IActionResult> GetCoupons(CancellationToken ct)
        {
            var result = await _couponService.GetCouponsAsync(ct);

            return Ok(new ApiResponse<List<CouponDTO>>
            {
                Success = true,
                Code = 200,
                SubCode = 0,
                UserMessage = "Get coupons successfully",
                Data = result,
                ServerTime = DateTimeOffset.Now
            });
        }
    }
}
