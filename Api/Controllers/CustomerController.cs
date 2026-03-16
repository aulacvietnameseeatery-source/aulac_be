using API.Models;
using Core.DTO.Customer;
using Core.DTO.General;
using Core.Interface.Service.Customer;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [Route("api/customers")]
    [ApiController]
    public class CustomerController : ControllerBase
    {
        private readonly ICustomerService _customerService;

        public CustomerController(ICustomerService customerService)
        {
            _customerService = customerService;
        }

        [HttpGet]
        public async Task<IActionResult> GetCustomers(
            [FromQuery] CustomerListQueryDTO query,
            CancellationToken ct)
        {
            var result = await _customerService.GetCustomersAsync(query, ct);

            return Ok(new ApiResponse<PagedResultDTO<CustomerListDTO>>
            {
                Success = true,
                Code = 200,
                SubCode = 0,
                UserMessage = "Get customers successfully",
                Data = result,
                ServerTime = DateTimeOffset.UtcNow
            });
        }

        [HttpGet("phone/{phone}")]
        public async Task<IActionResult> GetByPhone(string phone)
        {
            var customer = await _customerService.GetByPhoneAsync(phone);

            if (customer == null)
            {
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Code = 200,
                    UserMessage = "Customer not found.",
                    Data = new { },
                    ServerTime = DateTimeOffset.UtcNow
                });
            }

            return Ok(new ApiResponse<CustomerDto>
            {
                Success = true,
                Code = 200,
                UserMessage = "Customer retrieved successfully.",
                Data = customer,
                ServerTime = DateTimeOffset.UtcNow
            });
        }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken = default)
        {
            var customer = await _customerService.GetByIdAsync(id, cancellationToken);

            if (customer == null)
            {
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Code = 200,
                    UserMessage = "Customer not found.",
                    Data = new { },
                    ServerTime = DateTimeOffset.UtcNow
                });
            }

            return Ok(new ApiResponse<CustomerDto>
            {
                Success = true,
                Code = 200,
                UserMessage = "Customer retrieved successfully.",
                Data = customer,
                ServerTime = DateTimeOffset.UtcNow
            });
        }
    }
}
