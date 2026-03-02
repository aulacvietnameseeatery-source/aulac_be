using API.Models;
using Core.Attribute;
using Core.Data;
using Core.DTO.LookUpValue;
using Core.Interface.Service.Table;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// Manages the configurable lookup values used by tables: zones and table types.
/// All endpoints sit under <c>api/tables</c> to match the FE contract.
/// Statuses are NOT exposed — they are static on the FE.
/// </summary>
[ApiController]
[Route("api/tables")]
[Authorize]
public class TableLookupController : ControllerBase
{
    private readonly ITableService _tableService;

    public TableLookupController(ITableService tableService)
    {
        _tableService = tableService;
    }

    // ── Zones ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Gets all active zone lookup values ordered by sort order.
    /// Returns full i18n and descriptionI18n maps.
    /// </summary>
    [HttpGet("zones")]
    [HasPermission(Permissions.ViewTable)]
    [ProducesResponseType(typeof(ApiResponse<List<LookupValueI18nDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetZones(CancellationToken ct)
    {
        var data = await _tableService.GetZonesAsync(ct);

        return Ok(new ApiResponse<List<LookupValueI18nDto>>
        {
            Success = true,
            Code = 200,
            UserMessage = "Zones retrieved successfully.",
            Data = data,
            ServerTime = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Creates a new zone lookup value.
    /// ValueCode is auto-generated from ValueName (SCREAMING_SNAKE_CASE) when omitted.
    /// Accepts i18n and descriptionI18n maps for per-locale translations.
    /// </summary>
    [HttpPost("zones")]
    [HasPermission(Permissions.ManageTableZone)]
    [ProducesResponseType(typeof(ApiResponse<LookupValueI18nDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateZone([FromBody] CreateLookupValueRequest request, CancellationToken ct)
    {
        var dto = await _tableService.CreateZoneAsync(request, ct);

        return StatusCode(201, new ApiResponse<LookupValueI18nDto>
        {
            Success = true,
            Code = 201,
            UserMessage = $"Zone '{dto.ValueCode}' created successfully.",
            Data = dto,
            ServerTime = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Updates an existing zone lookup value (rename, reorder, update i18n/description).
    /// Locked zones cannot be modified.
    /// </summary>
    [HttpPut("zones/{valueId:int}")]
    [HasPermission(Permissions.ManageTableZone)]
    [ProducesResponseType(typeof(ApiResponse<LookupValueI18nDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateZone(uint valueId, [FromBody] UpdateLookupValueRequest request, CancellationToken ct)
    {
        var dto = await _tableService.UpdateZoneAsync(valueId, request, ct);

        return Ok(new ApiResponse<LookupValueI18nDto>
        {
            Success = true,
            Code = 200,
            UserMessage = "Zone updated successfully.",
            Data = dto,
            ServerTime = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Soft-deletes a zone lookup value.
    /// Returns 409 if the zone is still referenced by one or more tables.
    /// </summary>
    [HttpDelete("zones/{valueId:int}")]
    [HasPermission(Permissions.ManageTableZone)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteZone(uint valueId, CancellationToken ct)
    {
        await _tableService.DeleteZoneAsync(valueId, ct);
        return NoContent();
    }

    // ── Table Types ────────────────────────────────────────────────────────

    /// <summary>
    /// Gets all active table type lookup values ordered by sort order.
    /// Returns full i18n and descriptionI18n maps.
    /// </summary>
    [HttpGet("types")]
    [HasPermission(Permissions.ViewTable)]
    [ProducesResponseType(typeof(ApiResponse<List<LookupValueI18nDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTableTypes(CancellationToken ct)
    {
        var data = await _tableService.GetTableTypesAsync(ct);

        return Ok(new ApiResponse<List<LookupValueI18nDto>>
        {
            Success = true,
            Code = 200,
            UserMessage = "Table types retrieved successfully.",
            Data = data,
            ServerTime = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Creates a new table type lookup value.
    /// ValueCode is auto-generated from ValueName (SCREAMING_SNAKE_CASE) when omitted.
    /// </summary>
    [HttpPost("types")]
    [HasPermission(Permissions.ManageTableType)]
    [ProducesResponseType(typeof(ApiResponse<LookupValueI18nDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateTableType([FromBody] CreateLookupValueRequest request, CancellationToken ct)
    {
        var dto = await _tableService.CreateTableTypeAsync(request, ct);

        return StatusCode(201, new ApiResponse<LookupValueI18nDto>
        {
            Success = true,
            Code = 201,
            UserMessage = $"Table type '{dto.ValueCode}' created successfully.",
            Data = dto,
            ServerTime = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Updates an existing table type lookup value (rename, reorder, update i18n/description).
    /// Locked types cannot be modified.
    /// </summary>
    [HttpPut("types/{valueId:int}")]
    [HasPermission(Permissions.ManageTableType)]
    [ProducesResponseType(typeof(ApiResponse<LookupValueI18nDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateTableType(uint valueId, [FromBody] UpdateLookupValueRequest request, CancellationToken ct)
    {
        var dto = await _tableService.UpdateTableTypeAsync(valueId, request, ct);

        return Ok(new ApiResponse<LookupValueI18nDto>
        {
            Success = true,
            Code = 200,
            UserMessage = "Table type updated successfully.",
            Data = dto,
            ServerTime = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Soft-deletes a table type lookup value.
    /// Returns 409 if the type is still referenced by one or more tables.
    /// </summary>
    [HttpDelete("types/{valueId:int}")]
    [HasPermission(Permissions.ManageTableType)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteTableType(uint valueId, CancellationToken ct)
    {
        await _tableService.DeleteTableTypeAsync(valueId, ct);
        return NoContent();
    }
}
