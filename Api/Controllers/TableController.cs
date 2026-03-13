using API.Models;
using Core.Attribute;
using Core.Data;
using Core.DTO.Table;
using Core.Interface.Service.Entity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// Manages restaurant tables — CRUD, status transitions, online toggling, QR codes, and media.
/// </summary>
[ApiController]
[Route("api/tables")]
[Authorize]
public class TableController : ControllerBase
{
    private readonly ITableService _tableService;

    public TableController(ITableService tableService)
    {
        _tableService = tableService;
    }

    // ── List / Select ─────────────────────────────────────────────────────────

    /// <summary>
    /// Gets a paged list of tables for the management screen.
    /// Each table includes its images (public URLs), zone, type, and status.
    /// </summary>
    /// <param name="request">Paging and filter parameters</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paged list of tables</returns>
    /// <response code="200">Tables retrieved successfully</response>
    [HttpGet]
    [HasPermission(Permissions.ViewTable)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<TableManagementDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTables([FromQuery] GetTableManagementRequest request, CancellationToken ct)
    {
        var (items, totalCount) = await _tableService.GetTablesForManagementAsync(request, ct);

        var pageIndex = request.PageIndex > 0 ? request.PageIndex : 1;
        var pageSize = request.PageSize > 0 ? request.PageSize : 30;

        return Ok(new ApiResponse<PagedResult<TableManagementDto>>
        {
            Success = true,
            Code = 200,
            UserMessage = "Tables retrieved successfully.",
            Data = new PagedResult<TableManagementDto>
            {
                PageData = items,
                PageIndex = pageIndex,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPage = (int)Math.Ceiling((double)totalCount / pageSize)
            },
            ServerTime = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Gets a lightweight table list for dropdown / order-creation usage.
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of tables for selection</returns>
    /// <response code="200">Tables retrieved successfully</response>
    [HttpGet("select")]
    [HasPermission(Permissions.ViewTable)]
    [ProducesResponseType(typeof(ApiResponse<List<TableSelectDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTablesForSelect(CancellationToken ct)
    {
        var data = await _tableService.GetTablesForSelectAsync(ct);

        return Ok(new ApiResponse<List<TableSelectDto>>
        {
            Success = true,
            Code = 200,
            UserMessage = "Tables retrieved successfully.",
            Data = data,
            ServerTime = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Gets full detail for a single table including media, active orders, and upcoming reservations.
    /// </summary>
    /// <param name="id">Table ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Detailed table information</returns>
    /// <response code="200">Table retrieved successfully</response>
    /// <response code="404">Table not found</response>
    [HttpGet("{id:long}")]
    [HasPermission(Permissions.ViewTable)]
    [ProducesResponseType(typeof(ApiResponse<TableDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTable(long id, CancellationToken ct)
    {
        var dto = await _tableService.GetTableByIdAsync(id, ct);

        return Ok(new ApiResponse<TableDetailDto>
        {
            Success = true,
            Code = 200,
            UserMessage = "Table retrieved successfully.",
            Data = dto,
            ServerTime = DateTimeOffset.UtcNow
        });
    }

    // ── Table CRUD ────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new restaurant table.
    /// Optionally attach up to 5 images in the same request (multipart/form-data).
    /// All table fields are sent as normal form fields alongside the image files.
    /// </summary>
    /// <remarks>
    /// Example multipart fields:
    /// - TableCode (string, required)
    /// - Capacity (int, required, 1–50)
    /// - IsOnline (bool, default false)
    /// - StatusLvId, TypeLvId, ZoneLvId (uint, required)
    /// - Images[] (file[], optional, max 5 × 5 MB, image types only)
    /// </remarks>
    /// <param name="request">Table creation details</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created table</returns>
    /// <response code="201">Table created successfully</response>
    /// <response code="400">Validation error</response>
    /// <response code="409">Table code already exists</response>
    [HttpPost]
    [HasPermission(Permissions.CreateTable)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<TableDetailDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateTable([FromForm] CreateTableFormRequest request, CancellationToken ct)
    {
        var dto = await _tableService.CreateTableAsync(request, ct);

        return StatusCode(StatusCodes.Status201Created, new ApiResponse<TableDetailDto>
        {
            Success = true,
            Code = 201,
            UserMessage = $"Table '{dto.TableCode}' created successfully.",
            Data = dto,
            ServerTime = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Updates a table's data and/or images in one request (multipart/form-data).
    /// All data fields are optional — only provided fields are changed.
    /// New images are uploaded; existing images listed in RemovedImageIds are deleted.
    /// </summary>
    /// <remarks>
    /// Example multipart fields:
    /// - TableCode, Capacity, IsOnline, StatusLvId, TypeLvId, ZoneLvId (all optional)
    /// - Images[] (file[], optional, new images to add, max 5 × 5 MB, image types only)
    /// - RemovedImageIds (string, comma-separated MediaAsset IDs to delete, e.g. "10,11")
    /// </remarks>
    /// <param name="id">Table ID</param>
    /// <param name="request">Fields to update</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated table</returns>
    /// <response code="200">Table updated successfully</response>
    /// <response code="400">Validation error</response>
    /// <response code="404">Table not found</response>
    /// <response code="409">Table code already exists</response>
    [HttpPut("{id:long}")]
    [HasPermission(Permissions.UpdateTable)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<TableDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateTable(long id, [FromForm] UpdateTableFormRequest request, CancellationToken ct)
    {
        var dto = await _tableService.UpdateTableAsync(id, request, ct);

        return Ok(new ApiResponse<TableDetailDto>
        {
            Success = true,
            Code = 200,
            UserMessage = "Table updated successfully.",
            Data = dto,
            ServerTime = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Soft-deletes a table.
    /// Returns 409 if the table has active orders or upcoming reservations.
    /// </summary>
    /// <param name="id">Table ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <response code="204">Table deleted successfully</response>
    /// <response code="404">Table not found</response>
    /// <response code="409">Table has active orders or upcoming reservations</response>
    [HttpDelete("{id:long}")]
    [HasPermission(Permissions.DeleteTable)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteTable(long id, CancellationToken ct)
    {
        await _tableService.DeleteTableAsync(id, ct);
        return NoContent();
    }

    /// <summary>
    /// Transitions a table to a new status.
    /// Valid paths: AVAILABLE→OCCUPIED/RESERVED/LOCKED · OCCUPIED→LOCKED ·
    /// RESERVED→OCCUPIED/AVAILABLE · LOCKED→AVAILABLE.
    /// Returns 422 if the transition is not allowed.
    /// </summary>
    [HttpPatch("{id:long}/status")]
    [HasPermission(Permissions.UpdateTableStatus)]
    [ProducesResponseType(typeof(ApiResponse<TableManagementDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateTableStatus(long id, [FromBody] UpdateTableStatusRequest request, CancellationToken ct)
    {
        var dto = await _tableService.UpdateStatusAsync(id, request, ct);

        return Ok(new ApiResponse<TableManagementDto>
        {
            Success = true,
            Code = 200,
            UserMessage = "Table status updated successfully.",
            Data = dto,
            ServerTime = DateTimeOffset.UtcNow
        });
    }

    // ── Bulk / QR ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Bulk-sets the online/offline flag for every table in a zone.
    /// </summary>
    /// <param name="request">Zone ID and target online state</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Number of affected tables</returns>
    /// <response code="200">Bulk update completed</response>
    /// <response code="400">Invalid zone ID</response>
    [HttpPatch("bulk-online")]
    [HasPermission(Permissions.UpdateTable)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkOnline([FromBody] BulkOnlineRequest request, CancellationToken ct)
    {
        var affectedCount = await _tableService.BulkSetOnlineAsync(request, ct);

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Code = 200,
            UserMessage = $"{affectedCount} table(s) updated.",
            Data = new { affectedCount },
            ServerTime = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Regenerates the QR code for a table.
    /// </summary>
    /// <param name="id">Table ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>New QR code URL and image URL</returns>
    /// <response code="200">QR code regenerated successfully</response>
    /// <response code="404">Table not found</response>
    [HttpPost("{id:long}/qr-code")]
    [HasPermission(Permissions.UpdateTable)]
    [ProducesResponseType(typeof(ApiResponse<QrCodeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RegenerateQrCode(long id, CancellationToken ct)
    {
        var dto = await _tableService.RegenerateQrCodeAsync(id, ct);

        return Ok(new ApiResponse<QrCodeDto>
        {
            Success = true,
            Code = 200,
            UserMessage = "QR code regenerated successfully.",
            Data = dto,
            ServerTime = DateTimeOffset.UtcNow
        });
    }

    // ── Incremental media ─────────────────────────────────────────────────────

    /// <summary>
    /// Adds images to an existing table without changing any other data.
    /// Accepts up to 5 files, max 5 MB each, image types only.
    /// Use the PUT /{id} endpoint to add/remove images together with data changes.
    /// </summary>
    /// <param name="id">Table ID</param>
    /// <param name="files">Image files</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of created media records with public URLs</returns>
    /// <response code="201">Media uploaded successfully</response>
    /// <response code="400">No files provided, too many files, file too large, or unsupported type</response>
    /// <response code="404">Table not found</response>
    [HttpPost("{id:long}/media")]
    [HasPermission(Permissions.ManageTableMedia)]
    [ProducesResponseType(typeof(ApiResponse<List<TableMediaDto>>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadTableMedia(long id, [FromForm] List<IFormFile> files, CancellationToken ct)
    {
        if (files is not { Count: > 0 })
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Code = 400,
                UserMessage = "No files provided.",
                Data = new { },
                ServerTime = DateTimeOffset.UtcNow
            });

        var inputs = files.Select(f => new MediaFileInput
        {
            Stream = f.OpenReadStream(),
            FileName = f.FileName,
            ContentType = f.ContentType
        }).ToList();

        var result = await _tableService.UploadTableMediaAsync(id, inputs, ct);

        return StatusCode(StatusCodes.Status201Created, new ApiResponse<List<TableMediaDto>>
        {
            Success = true,
            Code = 201,
            UserMessage = $"{result.Count} image(s) uploaded successfully.",
            Data = result,
            ServerTime = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Removes a specific image from a table.
    /// To remove images together with data changes, use PUT /{id} with RemovedImageIds instead.
    /// </summary>
    /// <param name="id">Table ID</param>
    /// <param name="mediaId">Media asset ID to remove</param>
    /// <param name="ct">Cancellation token</param>
    /// <response code="204">Media deleted successfully</response>
    /// <response code="404">Table not found or media not linked to this table</response>
    [HttpDelete("{id:long}/media/{mediaId:long}")]
    [HasPermission(Permissions.ManageTableMedia)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTableMedia(long id, long mediaId, CancellationToken ct)
    {
        await _tableService.DeleteTableMediaAsync(id, mediaId, ct);
        return NoContent();
    }


    /// <summary>
    /// Gets available tables for a specific reservation time.
    /// </summary>
    /// <param name="targetTime">The time to check for overlap</param>
    /// <param name="ct">Cancellation token</param>
    [HttpGet("available")]
    // [HasPermission(Permissions.ViewTable)] // Mở comment nếu cần Auth
    [ProducesResponseType(typeof(ApiResponse<List<TableSelectDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAvailableTables([FromQuery] DateTime targetTime, CancellationToken ct)
    {
        var data = await _tableService.GetAvailableTablesByTimeAsync(targetTime, ct);

        return Ok(new ApiResponse<List<TableSelectDto>>
        {
            Success = true,
            Code = 200,
            UserMessage = "Available tables retrieved successfully.",
            Data = data,
            ServerTime = DateTimeOffset.UtcNow
        });
    }
}