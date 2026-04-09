using API.Models;
using API.Attributes;
using Core.Data;
using Core.DTO.Coupon;
using Core.DTO.General;
using Core.Interface.Service.Coupon;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [Route("api/coupons")]
    [ApiController]
    public class CouponController : ControllerBase
    {
        private readonly ICouponService _couponService;
        private readonly ILogger<CouponController> _logger;

        public CouponController(
            ICouponService couponService,
            ILogger<CouponController> logger)
        {
            _couponService = couponService;
            _logger = logger;
        }

        /// <summary>
        /// Get active coupons (for public use)
        /// </summary>
        /// <param name="customerId">Optional customer ID to filter applicable coupons</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>List of active coupons</returns>
        /// <response code="200">Coupons retrieved successfully</response>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<CouponDTO>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCoupons([FromQuery] long? customerId, CancellationToken ct)
        {
            var result = await _couponService.GetCouponsAsync(customerId, ct);

            return Ok(new ApiResponse<List<CouponDTO>>
            {
                Success = true,
                Code = 200,
                SubCode = 0,
                UserMessage = "Get coupons successfully",
                Data = result,
                ServerTime = DateTimeOffset.UtcNow
            });
        }

        /// <summary>
        /// Get paginated coupons list with filtering and search
        /// </summary>
        /// <param name="query">Filter and pagination parameters</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Paginated list of coupons</returns>
        /// <response code="200">Coupons list retrieved successfully</response>
        [HttpGet("list")]
        [HasPermission(Permissions.ViewCoupon)]
        [ProducesResponseType(typeof(ApiResponse<PagedResultDTO<CouponDTO>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllCoupons(
            [FromQuery] CouponListQueryDTO query,
            CancellationToken ct)
        {
            var result = await _couponService.GetAllCouponsAsync(query, ct);

            return Ok(new ApiResponse<PagedResultDTO<CouponDTO>>
            {
                Success = true,
                Code = 200,
                SubCode = 0,
                UserMessage = "Get coupons list successfully",
                Data = result,
                ServerTime = DateTimeOffset.UtcNow
            });
        }

        /// <summary>
        /// Get coupon by ID for edit form
        /// </summary>
        /// <param name="id">Coupon ID</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Coupon detail</returns>
        /// <response code="200">Coupon found</response>
        /// <response code="404">Coupon not found</response>
        [HttpGet("{id}")]
        [HasPermission(Permissions.ViewCoupon)]
        [ProducesResponseType(typeof(ApiResponse<CouponDetailDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCouponById(
            long id,
            CancellationToken ct)
        {
            try
            {
                var coupon = await _couponService.GetCouponDetailAsync(id, ct);

                return Ok(new ApiResponse<CouponDetailDto>
                {
                    Success = true,
                    Code = 200,
                    UserMessage = "Coupon retrieved successfully.",
                    Data = coupon,
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

        /// <summary>
        /// Create a new coupon
        /// </summary>
        /// <param name="request">Coupon creation request</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Created coupon</returns>
        /// <response code="201">Coupon created successfully</response>
        /// <response code="400">Invalid request</response>
        /// <response code="409">Coupon code already exists</response>
        [HttpPost]
        [HasPermission(Permissions.CreateCoupon)]
        [ProducesResponseType(typeof(ApiResponse<CouponDTO>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateCoupon(
            [FromBody] CreateCouponRequest request,
            CancellationToken ct)
        {
            try
            {
                var coupon = await _couponService.CreateCouponAsync(request, ct);

                return CreatedAtAction(
                    nameof(GetCouponById),
                    new { id = coupon.CouponId },
                    new ApiResponse<CouponDTO>
                    {
                        Success = true,
                        Code = 201,
                        UserMessage = "Coupon created successfully.",
                        Data = coupon,
                        ServerTime = DateTimeOffset.UtcNow
                    });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
            {
                _logger.LogWarning(ex, "Failed to create coupon");
                return Conflict(new ApiResponse<object>
                {
                    Success = false,
                    Code = 409,
                    UserMessage = ex.Message,
                    Data = default!,
                    ServerTime = DateTimeOffset.UtcNow
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Failed to create coupon");
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Code = 400,
                    UserMessage = ex.Message,
                    Data = default!,
                    ServerTime = DateTimeOffset.UtcNow
                });
            }
        }

        /// <summary>
        /// Update an existing coupon
        /// </summary>
        /// <param name="id">Coupon ID</param>
        /// <param name="request">Coupon update request</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Updated coupon</returns>
        /// <response code="200">Coupon updated successfully</response>
        /// <response code="400">Invalid request</response>
        /// <response code="404">Coupon not found</response>
        /// <response code="409">Coupon code already exists</response>
        [HttpPut("{id}")]
        [HasPermission(Permissions.EditCoupon)]
        [ProducesResponseType(typeof(ApiResponse<CouponDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateCoupon(
            long id,
            [FromBody] UpdateCouponRequest request,
            CancellationToken ct)
        {
            try
            {
                var coupon = await _couponService.UpdateCouponAsync(id, request, ct);

                return Ok(new ApiResponse<CouponDTO>
                {
                    Success = true,
                    Code = 200,
                    UserMessage = "Coupon updated successfully.",
                    Data = coupon,
                    ServerTime = DateTimeOffset.UtcNow
                });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Coupon not found");
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Code = 404,
                    UserMessage = ex.Message,
                    Data = default!,
                    ServerTime = DateTimeOffset.UtcNow
                });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
            {
                _logger.LogWarning(ex, "Failed to update coupon");
                return Conflict(new ApiResponse<object>
                {
                    Success = false,
                    Code = 409,
                    UserMessage = ex.Message,
                    Data = default!,
                    ServerTime = DateTimeOffset.UtcNow
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Failed to update coupon");
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Code = 400,
                    UserMessage = ex.Message,
                    Data = default!,
                    ServerTime = DateTimeOffset.UtcNow
                });
            }
        }

        /// <summary>
        /// Delete a coupon
        /// </summary>
        /// <param name="id">Coupon ID</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>No content if successful</returns>
        /// <response code="204">Coupon deleted successfully</response>
        /// <response code="400">Coupon has dependencies and cannot be deleted</response>
        /// <response code="404">Coupon not found</response>
        [HttpDelete("{id}")]
        [HasPermission(Permissions.DeleteCoupon)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteCoupon(
            long id,
            CancellationToken ct)
        {
            try
            {
                await _couponService.DeleteCouponAsync(id, ct);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Coupon not found");
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
                _logger.LogWarning(ex, "Cannot delete coupon");
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Code = 400,
                    UserMessage = ex.Message,
                    Data = default!,
                    ServerTime = DateTimeOffset.UtcNow
                });
            }
        }

        /// <summary>
        /// Disable a coupon
        /// </summary>
        /// <param name="id">Coupon ID</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Success status</returns>
        /// <response code="200">Coupon disabled successfully</response>
        /// <response code="404">Coupon not found</response>
        [HttpPut("{id}/disable")]
        [HasPermission(Permissions.EditCoupon)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DisableCoupon(long id, CancellationToken ct)
        {
            try
            {
                await _couponService.DisableCouponAsync(id, ct);
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Code = 200,
                    UserMessage = "Coupon disabled successfully.",
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

        /// <summary>
        /// Activate a coupon
        /// </summary>
        /// <param name="id">Coupon ID</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Success status</returns>
        /// <response code="200">Coupon activated successfully</response>
        /// <response code="404">Coupon not found</response>
        [HttpPut("{id}/activate")]
        [HasPermission(Permissions.EditCoupon)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ActivateCoupon(long id, CancellationToken ct)
        {
            try
            {
                await _couponService.ActivateCouponAsync(id, ct);
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Code = 200,
                    UserMessage = "Coupon activated successfully.",
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
}
