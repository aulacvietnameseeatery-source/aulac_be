using API.Models;
using Core.Attribute;
using Core.Data;
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
        private readonly ILogger<CustomerController> _logger;

        public CustomerController(ICustomerService customerService, ILogger<CustomerController> logger)
        {
            _customerService = customerService;
            _logger = logger;
        }

        [HttpGet]
        [HasPermission(Permissions.ViewCustomer)]
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
        [HasPermission(Permissions.ViewCustomer)]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken = default)
        {
            var customer = await _customerService.GetByIdAsync(id, cancellationToken);

            if (customer == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Code = 404,
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

        [HttpPost]
        [HasPermission(Permissions.CreateCustomer)]
        public async Task<IActionResult> CreateCustomer(
            [FromBody] CreateCustomerRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var customer = await _customerService.CreateCustomerAsync(request, cancellationToken);

                return CreatedAtAction(
                    nameof(GetById),
                    new { id = customer.CustomerId },
                    new ApiResponse<CustomerDto>
                    {
                        Success = true,
                        Code = 201,
                        UserMessage = "Customer created successfully.",
                        Data = customer,
                        ServerTime = DateTimeOffset.UtcNow
                    });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Failed to create customer");
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

        [HttpPut("{id:long}")]
        [HasPermission(Permissions.UpdateCustomer)]
        public async Task<IActionResult> UpdateCustomer(
            long id,
            [FromBody] UpdateCustomerRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var customer = await _customerService.UpdateCustomerAsync(id, request, cancellationToken);

                return Ok(new ApiResponse<CustomerDto>
                {
                    Success = true,
                    Code = 200,
                    UserMessage = "Customer updated successfully.",
                    Data = customer,
                    ServerTime = DateTimeOffset.UtcNow
                });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Customer not found");
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
                _logger.LogWarning(ex, "Failed to update customer");
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

        [HttpDelete("{id:long}")]
        [HasPermission(Permissions.DeleteCustomer)]
        public async Task<IActionResult> DeleteCustomer(
            long id,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _customerService.DeleteCustomerAsync(id, cancellationToken);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Customer not found");
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
                _logger.LogWarning(ex, "Cannot delete customer");
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

        [HttpGet("detail/{customerId}")]
        public async Task<IActionResult> GetCustomerDetail(
        long customerId,
        CancellationToken ct)
        {
            var result = await _customerService
                .GetCustomerDetailAsync(customerId, ct);

            return Ok(new ApiResponse<CustomerDetailDTO?>
            {
                Success = true,
                Code = 200,
                SubCode = 0,
                UserMessage = "Get customer detail successfully",
                Data = result,
                ServerTime = DateTimeOffset.UtcNow
            });
        }

        [HttpGet("{customerId}/orders")]
        public async Task<IActionResult> GetCustomerOrders(
            long customerId,
            [FromQuery] CustomerOrderQueryDTO query,
            CancellationToken ct)
        {
            query.CustomerId = customerId;

            var result = await _customerService
                .GetCustomerOrdersAsync(query, ct);

            return Ok(new ApiResponse<PagedResultDTO<CustomerOrderDTO>>
            {
                Success = true,
                Code = 200,
                SubCode = 0,
                UserMessage = "Get customer orders successfully",
                Data = result,
                ServerTime = DateTimeOffset.UtcNow
            });
        }

        [HttpGet("{customerId}/orders/{orderId}")]
        public async Task<IActionResult> GetCustomerOrderDetail(
            long customerId,
            long orderId,
            CancellationToken ct)
        {
            var result = await _customerService
                .GetCustomerOrderDetailAsync(customerId, orderId, ct);

            return Ok(new ApiResponse<CustomerOrderDetailDTO?>
            {
                Success = true,
                Code = 200,
                SubCode = 0,
                UserMessage = "Get customer order detail successfully",
                Data = result,
                ServerTime = DateTimeOffset.UtcNow
            });
        }
    }
}
