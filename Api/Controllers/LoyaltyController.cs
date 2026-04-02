using API.Attributes;
using API.Models;
using Core.Data;
using Core.DTO.Loyalty;
using Core.Interface.Service.Loyalty;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Route("api/loyalty")]
[ApiController]
public class LoyaltyController : ControllerBase
{
    private readonly ILoyaltyService _loyaltyService;
    private readonly ILogger<LoyaltyController> _logger;

    public LoyaltyController(ILoyaltyService loyaltyService, ILogger<LoyaltyController> logger)
    {
        _loyaltyService = loyaltyService;
        _logger = logger;
    }

    [HttpPost("exchange")]
    [HasPermission(Permissions.UpdateCustomer)]
    public async Task<IActionResult> ExchangePoints([FromBody] LoyaltyExchangeRequest request, CancellationToken ct = default)
    {
        try
        {
            var result = await _loyaltyService.ExchangePointsToCouponAsync(request, ct);

            return Ok(new ApiResponse<LoyaltyExchangeResultDto>
            {
                Success = true,
                Code = 200,
                UserMessage = "Loyalty points exchanged successfully.",
                Data = result,
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
                Data = new { },
                ServerTime = DateTimeOffset.UtcNow
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Code = 400,
                UserMessage = ex.Message,
                Data = new { },
                ServerTime = DateTimeOffset.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exchanging loyalty points for customer {CustomerId}", request.CustomerId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Code = 500,
                UserMessage = "An error occurred while exchanging loyalty points.",
                Data = new { },
                ServerTime = DateTimeOffset.UtcNow
            });
        }
    }

    [HttpGet("customer/{id:long}/coupons")]
    [HasPermission(Permissions.ViewCustomer)]
    public async Task<IActionResult> GetCustomerCoupons(long id, CancellationToken ct = default)
    {
        try
        {
            var result = await _loyaltyService.GetCustomerCouponsAsync(id, ct);

            return Ok(new ApiResponse<List<LoyaltyCouponHistoryDto>>
            {
                Success = true,
                Code = 200,
                UserMessage = "Customer loyalty coupons retrieved successfully.",
                Data = result,
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
                Data = new { },
                ServerTime = DateTimeOffset.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading loyalty coupons for customer {CustomerId}", id);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Code = 500,
                UserMessage = "An error occurred while retrieving loyalty coupons.",
                Data = new { },
                ServerTime = DateTimeOffset.UtcNow
            });
        }
    }
}