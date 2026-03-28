using API.Models;
using Core.Attribute;
using Core.Data;
using Core.DTO.General;
using Core.DTO.Order;
using Core.DTO.Payment;
using Core.Interface.Service.Entity;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Route("api/payments")]
[ApiController]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(IPaymentService paymentService, ILogger<PaymentController> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    [HttpGet]
    //[HasPermission(Permissions.ViewPayment)]
    public async Task<IActionResult> GetPayments(
    [FromQuery] PaymentListQueryDTO query,
    CancellationToken ct)
    {
        var result = await _paymentService.GetPaymentsAsync(query, ct);

        return Ok(new ApiResponse<PagedResultDTO<PaymentListDTO>>
        {
            Success = true,
            Code = 200,
            Data = result,
            ServerTime = DateTimeOffset.UtcNow
        });
    }

    [HttpPost]
    [HasPermission(Permissions.ProcessPayment)]
    public async Task<IActionResult> ProcessPayment([FromBody] CreatePaymentDTO dto, CancellationToken cancellationToken = default)
    {
        try
        {
            await _paymentService.ProcessPaymentAsync(dto, cancellationToken);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Code = 200,
                UserMessage = "Payment processed successfully.",
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
                ServerTime = DateTimeOffset.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment for order {OrderId}", dto.OrderId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Code = 500,
                UserMessage = "An error occurred while processing the payment.",
                ServerTime = DateTimeOffset.UtcNow
            });
        }
    }
}
