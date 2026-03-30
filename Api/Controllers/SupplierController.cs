using API.Models;
using API.Attributes;
using Core.Data;
using Core.DTO.General;
using Core.DTO.Supplier;
using Core.Interface.Service.Entity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>
/// Supplier controller providing endpoints for supplier management operations
/// </summary>
[ApiController]
[Route("api/suppliers")]
public class SupplierController : ControllerBase
{
    private readonly ISupplierService _supplierService;
    private readonly ILogger<SupplierController> _logger;

    public SupplierController(
        ISupplierService supplierService,
        ILogger<SupplierController> logger)
    {
        _supplierService = supplierService;
        _logger = logger;
    }

    /// <summary>
    /// Get paginated suppliers with filtering and search
    /// </summary>
    /// <param name="query">Query parameters including pagination and search</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of suppliers</returns>
    /// <response code="200">Suppliers retrieved successfully</response>
    [HttpGet("list")]
    [HasPermission(Permissions.ViewSupplier)]
    [ProducesResponseType(typeof(ApiResponse<PagedResultDTO<SupplierDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllSuppliers(
        [FromQuery] SupplierListQueryDTO query,
        CancellationToken cancellationToken = default)
    {
        var result = await _supplierService.GetAllSuppliersAsync(query, cancellationToken);

        return Ok(new ApiResponse<PagedResultDTO<SupplierDto>>
        {
            Success = true,
            Code = 200,
            SubCode = 0,
            UserMessage = "Get suppliers successfully",
            Data = result,
            ServerTime = DateTimeOffset.Now
        });
    }

    /// <summary>
    /// Get supplier by ID for edit form
    /// </summary>
    /// <param name="id">Supplier ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Supplier detail</returns>
    /// <response code="200">Supplier found</response>
    /// <response code="404">Supplier not found</response>
    [HttpGet("{id}")]
    [HasPermission(Permissions.ViewSupplier)]
    [ProducesResponseType(typeof(ApiResponse<SupplierDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSupplierById(
        long id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var supplier = await _supplierService.GetSupplierDetailAsync(id, cancellationToken);

            return Ok(new ApiResponse<SupplierDetailDto>
            {
                Success = true,
                Code = 200,
                UserMessage = "Supplier retrieved successfully.",
                Data = supplier,
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
    /// Create a new supplier
    /// </summary>
    /// <param name="request">Supplier creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created supplier</returns>
    /// <response code="201">Supplier created successfully</response>
    /// <response code="400">Invalid request or supplier name already exists</response>
    [HttpPost]
    [HasPermission(Permissions.CreateSupplier)]
    [ProducesResponseType(typeof(ApiResponse<SupplierDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSupplier(
        [FromBody] CreateSupplierRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var supplier = await _supplierService.CreateSupplierAsync(request, cancellationToken);

            return CreatedAtAction(
                nameof(GetSupplierById),
                new { id = supplier.SupplierId },
                new ApiResponse<SupplierDto>
                {
                    Success = true,
                    Code = 201,
                    UserMessage = "Supplier created successfully.",
                    Data = supplier,
                    ServerTime = DateTimeOffset.UtcNow
                });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to create supplier");
            return Conflict(new ApiResponse<object>
            {
                Success = false,
                Code = 409,
                UserMessage = ex.Message,
                SystemMessage = "SUPPLIER_NAME_EXISTS",
                Data = default!,
                ServerTime = DateTimeOffset.UtcNow
            });
        }
    }

    /// <summary>
    /// Update an existing supplier
    /// </summary>
    /// <param name="id">Supplier ID</param>
    /// <param name="request">Supplier update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated supplier</returns>
    /// <response code="200">Supplier updated successfully</response>
    /// <response code="400">Invalid request or supplier name already exists</response>
    /// <response code="404">Supplier not found</response>
    [HttpPut("{id}")]
    [HasPermission(Permissions.EditSupplier)]
    [ProducesResponseType(typeof(ApiResponse<SupplierDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSupplier(
        long id,
        [FromBody] UpdateSupplierRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var supplier = await _supplierService.UpdateSupplierAsync(id, request, cancellationToken);

            return Ok(new ApiResponse<SupplierDto>
            {
                Success = true,
                Code = 200,
                UserMessage = "Supplier updated successfully.",
                Data = supplier,
                ServerTime = DateTimeOffset.UtcNow
            });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Supplier not found");
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
            _logger.LogWarning(ex, "Failed to update supplier");
            return Conflict(new ApiResponse<object>
            {
                Success = false,
                Code = 409,
                UserMessage = ex.Message,
                SystemMessage = "SUPPLIER_NAME_EXISTS",
                Data = default!,
                ServerTime = DateTimeOffset.UtcNow
            });
        }
    }

    /// <summary>
    /// Delete a supplier
    /// </summary>
    /// <param name="id">Supplier ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content if successful</returns>
    /// <response code="204">Supplier deleted successfully</response>
    /// <response code="400">Supplier has dependencies and cannot be deleted</response>
    /// <response code="404">Supplier not found</response>
    [HttpDelete("{id}")]
    [HasPermission(Permissions.DeleteSupplier)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSupplier(
        long id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _supplierService.DeleteSupplierAsync(id, cancellationToken);

            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Supplier not found");
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
            _logger.LogWarning(ex, "Cannot delete supplier");
            return Conflict(new ApiResponse<object>
            {
                Success = false,
                Code = 409,
                UserMessage = ex.Message,
                SystemMessage = "SUPPLIER_HAS_DEPENDENCIES",
                Data = default!,
                ServerTime = DateTimeOffset.UtcNow
            });
        }
    }
}
