using API.Models;
using Core.Attribute;
using Core.Data;
using Core.DTO.LookUpValue;
using Core.DTO.Table;
using Core.Interface.Service.Table;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// Manages the configurable lookup values used by tables: zones and table types.
/// All endpoints sit under <c>api/tables</c> to match the FE contract.
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
    /// Used to populate zone tabs in the filter bar, the zone dropdown in the table
    /// modal, and the zone section grouping on the main page.
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of zone lookup values with i18n translations</returns>
    /// <response code="200">Zones retrieved successfully</response>
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
    /// Called from the "Add Zone" modal and from the ALCombobox quick-add in the table form.
    /// <c>ValueCode</c> is auto-generated from <c>ValueName</c> (SCREAMING_SNAKE_CASE) when omitted.
    /// </summary>
    /// <param name="request">Zone creation details</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Newly created zone</returns>
    /// <response code="201">Zone created successfully</response>
    /// <response code="400">Validation error (blank name, name too long)</response>
    /// <response code="409">Zone with same name or code already exists</response>
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
    /// Updates an existing zone lookup value (rename, reorder, update description).
    /// Locked or system-seeded zones cannot be modified.
    /// </summary>
    /// <param name="valueId">Zone lookup value ID</param>
    /// <param name="request">Fields to update — all optional</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated zone</returns>
    /// <response code="200">Zone updated successfully</response>
    /// <response code="400">Blank name supplied or zone is locked</response>
    /// <response code="404">Zone not found</response>
    /// <response code="409">Another zone with the same name already exists</response>
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
    /// Locked or system-seeded zones cannot be deleted.
    /// </summary>
    /// <param name="valueId">Zone lookup value ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <response code="200">Zone deleted successfully</response>
    /// <response code="400">Zone is locked</response>
    /// <response code="404">Zone not found</response>
    /// <response code="409">Zone is in use by one or more tables</response>
    [HttpDelete("zones/{valueId:int}")]
    [HasPermission(Permissions.ManageTableZone)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteZone(uint valueId, CancellationToken ct)
    {
        await _tableService.DeleteZoneAsync(valueId, ct);

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Code = 200,
            UserMessage = "Zone deleted successfully.",
            Data = new { },
            ServerTime = DateTimeOffset.UtcNow
        });
    }

    // ── Table Types ────────────────────────────────────────────────────────

    /// <summary>
    /// Gets all active table type lookup values ordered by sort order.
    /// Used to populate the type dropdown in the table modal and the type filter in the filter bar.
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of table type lookup values with i18n translations</returns>
    /// <response code="200">Table types retrieved successfully</response>
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
    /// Called from the ALCombobox quick-add in the table form.
    /// <c>ValueCode</c> is auto-generated from <c>ValueName</c> (SCREAMING_SNAKE_CASE) when omitted.
    /// </summary>
    /// <param name="request">Table type creation details</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Newly created table type</returns>
    /// <response code="201">Table type created successfully</response>
    /// <response code="400">Validation error (blank name, name too long)</response>
    /// <response code="409">Table type with same name or code already exists</response>
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
    /// Updates an existing table type lookup value (rename, reorder, update description).
    /// Locked or system-seeded types cannot be modified.
    /// </summary>
    /// <param name="valueId">Table type lookup value ID</param>
    /// <param name="request">Fields to update — all optional</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated table type</returns>
    /// <response code="200">Table type updated successfully</response>
    /// <response code="400">Blank name supplied or type is locked</response>
    /// <response code="404">Table type not found</response>
    /// <response code="409">Another type with the same name already exists</response>
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
    /// Locked or system-seeded types cannot be deleted.
    /// </summary>
    /// <param name="valueId">Table type lookup value ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <response code="200">Table type deleted successfully</response>
    /// <response code="400">Type is locked</response>
    /// <response code="404">Table type not found</response>
    /// <response code="409">Type is in use by one or more tables</response>
    [HttpDelete("types/{valueId:int}")]
    [HasPermission(Permissions.ManageTableType)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteTableType(uint valueId, CancellationToken ct)
    {
        await _tableService.DeleteTableTypeAsync(valueId, ct);

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Code = 200,
            UserMessage = "Table type deleted successfully.",
            Data = new { },
            ServerTime = DateTimeOffset.UtcNow
        });
    }
}
