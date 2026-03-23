using API.Models;
using Core.Attribute;
using Core.Data;
using Core.DTO.Tax;
using Core.Interface.Service.Entity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Api.Controllers;

/// <summary>
/// Tax controller providing endpoints for tax management operations
/// </summary>
[Route("api/taxes")]
[ApiController]
public class TaxController : ControllerBase
{
    private readonly ITaxService _taxService;
    private readonly ILogger<TaxController> _logger;

    public TaxController(ITaxService taxService, ILogger<TaxController> logger)
    {
        _taxService = taxService;
        _logger = logger;
    }

    /// <summary>
    /// Get all taxes
    /// </summary>
    /// <param name="onlyActive">Filter by active status</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of taxes</returns>
    /// <response code="200">Taxes retrieved successfully</response>
    [HttpGet]
    [HasPermission(Permissions.ViewSystemSettings)]
    [ProducesResponseType(typeof(ApiResponse<List<TaxDTO>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllTaxes([FromQuery] bool onlyActive = true, CancellationToken ct = default)
    {
        var result = await _taxService.GetAllTaxesAsync(onlyActive, ct);
        return Ok(new ApiResponse<List<TaxDTO>>
        {
            Success = true,
            Code = 200,
            UserMessage = "Get all taxes successfully.",
            Data = result,
            ServerTime = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Get tax by ID
    /// </summary>
    /// <param name="id">Tax ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Tax detail</returns>
    /// <response code="200">Tax found</response>
    /// <response code="404">Tax not found</response>
    [HttpGet("{id:long}")]
    [HasPermission(Permissions.ViewSystemSettings)]
    [ProducesResponseType(typeof(ApiResponse<TaxDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTaxById(long id, CancellationToken ct = default)
    {
        var result = await _taxService.GetTaxByIdAsync(id, ct);
        if (result == null)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Code = 404,
                UserMessage = $"Tax with id {id} not found.",
                Data = default!,
                ServerTime = DateTimeOffset.UtcNow
            });
        }

        return Ok(new ApiResponse<TaxDTO>
        {
            Success = true,
            Code = 200,
            UserMessage = "Get tax by id successfully.",
            Data = result,
            ServerTime = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Get default tax
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Default tax detail</returns>
    /// <response code="200">Default tax found or null</response>
    [HttpGet("default")]
    [HasPermission(Permissions.ViewSystemSettings)]
    [ProducesResponseType(typeof(ApiResponse<TaxDTO?>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDefaultTax(CancellationToken ct = default)
    {
        var result = await _taxService.GetDefaultTaxAsync(ct);
        return Ok(new ApiResponse<TaxDTO?>
        {
            Success = true,
            Code = 200,
            UserMessage = "Get default tax successfully.",
            Data = result,
            ServerTime = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Create a new tax
    /// </summary>
    /// <param name="request">Create tax request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Tax ID</returns>
    /// <response code="201">Tax created successfully</response>
    /// <response code="400">Invalid request</response>
    [HttpPost]
    [HasPermission(Permissions.ManageSystemSettings)]
    [ProducesResponseType(typeof(ApiResponse<long>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateTax([FromBody] CreateTaxRequestDTO request, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Code = 400,
                UserMessage = "Invalid request data.",
                Data = ModelState,
                ServerTime = DateTimeOffset.UtcNow
            });
        }

        try
        {
            var id = await _taxService.CreateTaxAsync(request, ct);
            
            _logger.LogInformation("Created new tax: {TaxName} (ID: {TaxId})", request.TaxName, id);

            return StatusCode(StatusCodes.Status201Created, new ApiResponse<long>
            {
                Success = true,
                Code = 201,
                UserMessage = "Tax created successfully.",
                Data = id,
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
                Data = default!,
                ServerTime = DateTimeOffset.UtcNow
            });
        }
    }

    /// <summary>
    /// Update an existing tax
    /// </summary>
    /// <param name="id">Tax ID</param>
    /// <param name="request">Update tax request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Result of the update</returns>
    /// <response code="200">Tax updated successfully</response>
    /// <response code="400">Invalid request</response>
    /// <response code="404">Tax not found</response>
    [HttpPatch("{id:long}")]
    [HasPermission(Permissions.ManageSystemSettings)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTax(long id, [FromBody] UpdateTaxRequestDTO request, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Code = 400,
                UserMessage = "Invalid request data.",
                Data = ModelState,
                ServerTime = DateTimeOffset.UtcNow
            });
        }

        try
        {
            await _taxService.UpdateTaxAsync(id, request, ct);
            
            _logger.LogInformation("Updated tax (ID: {TaxId})", id);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Code = 200,
                UserMessage = "Tax updated successfully.",
                Data = default!,
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
        catch (InvalidOperationException ex)
        {
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
    /// Delete a tax
    /// </summary>
    /// <param name="id">Tax ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Result of the deletion</returns>
    /// <response code="200">Tax deleted successfully</response>
    /// <response code="404">Tax not found</response>
    [HttpDelete("{id:long}")]
    [HasPermission(Permissions.ManageSystemSettings)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTax(long id, CancellationToken ct = default)
    {
        try
        {
            await _taxService.DeleteTaxAsync(id, ct);
            
            _logger.LogInformation("Deleted tax (ID: {TaxId})", id);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Code = 200,
                UserMessage = "Tax deleted successfully.",
                Data = default!,
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
        catch (InvalidOperationException ex)
        {
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
}
