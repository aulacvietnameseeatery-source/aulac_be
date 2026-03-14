using API.Models;
using Core.Attribute;
using Core.Data;
using Core.DTO.LookUpValue;
using Core.Interface.Service.LookUp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// General CRUD controller for managing lookup values of any type.
/// </summary>
[ApiController]
[Route("api/lookups/{typeId:int}")]
[Authorize]
public class LookupController : ControllerBase
{
    private readonly ILookupService _lookupService;

    public LookupController(ILookupService lookupService)
    {
        _lookupService = lookupService;
    }

    /// <summary>
    /// Gets all active lookup values for the specified type, ordered by sort order.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<LookupValueI18nDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetValues(ushort typeId, CancellationToken ct)
    {
        var data = await _lookupService.GetAllActiveByTypeAsync(typeId, ct);

        return Ok(new ApiResponse<List<LookupValueI18nDto>>
        {
            Success = true,
            Code = 200,
            UserMessage = "Lookup values retrieved successfully.",
            Data = data,
            ServerTime = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Creates a new lookup value for the specified type.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<LookupValueI18nDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateValue(ushort typeId, [FromBody] CreateLookupValueRequest request, CancellationToken ct)
    {
        var dto = await _lookupService.CreateAsync(typeId, request, ct);

        return StatusCode(201, new ApiResponse<LookupValueI18nDto>
        {
            Success = true,
            Code = 201,
            UserMessage = $"Lookup value '{dto.ValueCode}' created successfully.",
            Data = dto,
            ServerTime = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Updates an existing lookup value.
    /// </summary>
    [HttpPut("{valueId:int}")]
    [ProducesResponseType(typeof(ApiResponse<LookupValueI18nDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateValue(ushort typeId, uint valueId, [FromBody] UpdateLookupValueRequest request, CancellationToken ct)
    {
        var dto = await _lookupService.UpdateAsync(typeId, valueId, request, ct);

        return Ok(new ApiResponse<LookupValueI18nDto>
        {
            Success = true,
            Code = 200,
            UserMessage = "Lookup value updated successfully.",
            Data = dto,
            ServerTime = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Soft-deletes a lookup value.
    /// </summary>
    [HttpDelete("{valueId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteValue(ushort typeId, uint valueId, [FromQuery] string typeLabel = "lookup", CancellationToken ct = default)
    {
        await _lookupService.DeleteAsync(typeId, valueId, typeLabel, ct);
        return NoContent();
    }
}
