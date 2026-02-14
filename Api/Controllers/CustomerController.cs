using API.Models;
using Core.DTO.Customer;
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
    }
}
