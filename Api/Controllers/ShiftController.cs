using API.Models;
using API.Attributes;
using Core.Data;
using Core.DTO.Shift;
using Core.Interface.Service.Shift;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers;

/// <summary>
/// Unified shift management: templates, assignments, attendance tracking, and reports.
/// </summary>
[ApiController]
[Route("api/shifts")]
[Authorize]
public class ShiftController : ControllerBase
{
    private readonly IShiftTemplateService _templateService;
    private readonly IShiftAssignmentService _assignmentService;
    private readonly IAttendanceService _attendanceService;

    public ShiftController(
        IShiftTemplateService templateService,
        IShiftAssignmentService assignmentService,
        IAttendanceService attendanceService)
    {
        _templateService = templateService;
        _assignmentService = assignmentService;
        _attendanceService = attendanceService;
    }

    private long GetStaffId() =>
        long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("Staff ID not found in token"));

    // ─────────────────────────────────────────────────────────────
    //  TEMPLATES
    // ─────────────────────────────────────────────────────────────

    /// <summary>Lists all shift templates, optionally filtered by active status.</summary>
    [HttpGet("templates")]
    [HasPermission(Permissions.ViewShift)]
    [ProducesResponseType(typeof(ApiResponse<List<ShiftTemplateListDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTemplates([FromQuery] bool? isActive, CancellationToken ct)
    {
        var items = await _templateService.GetAllAsync(isActive, ct);
        return Ok(new ApiResponse<List<ShiftTemplateListDto>>
        {
            Success = true, Code = 200,
            UserMessage = "Shift templates retrieved successfully.",
            Data = items, ServerTime = DateTimeOffset.UtcNow
        });
    }

    /// <summary>Gets full detail for a single shift template.</summary>
    [HttpGet("templates/{id:long}")]
    [HasPermission(Permissions.ViewShift)]
    [ProducesResponseType(typeof(ApiResponse<ShiftTemplateDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTemplate(long id, CancellationToken ct)
    {
        var dto = await _templateService.GetByIdAsync(id, ct);
        return Ok(new ApiResponse<ShiftTemplateDetailDto>
        {
            Success = true, Code = 200,
            UserMessage = "Shift template retrieved successfully.",
            Data = dto, ServerTime = DateTimeOffset.UtcNow
        });
    }

    /// <summary>Creates a new shift template.</summary>
    [HttpPost("templates")]
    [HasPermission(Permissions.ManageShiftTemplate)]
    [ProducesResponseType(typeof(ApiResponse<ShiftTemplateDetailDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateTemplate([FromBody] CreateShiftTemplateRequest request, CancellationToken ct)
    {
        var dto = await _templateService.CreateAsync(request, GetStaffId(), ct);
        return StatusCode(StatusCodes.Status201Created, new ApiResponse<ShiftTemplateDetailDto>
        {
            Success = true, Code = 201,
            UserMessage = "Shift template created successfully.",
            Data = dto, ServerTime = DateTimeOffset.UtcNow
        });
    }

    /// <summary>Updates an existing shift template.</summary>
    [HttpPut("templates/{id:long}")]
    [HasPermission(Permissions.ManageShiftTemplate)]
    [ProducesResponseType(typeof(ApiResponse<ShiftTemplateDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateTemplate(long id, [FromBody] UpdateShiftTemplateRequest request, CancellationToken ct)
    {
        var dto = await _templateService.UpdateAsync(id, request, GetStaffId(), ct);
        return Ok(new ApiResponse<ShiftTemplateDetailDto>
        {
            Success = true, Code = 200,
            UserMessage = "Shift template updated successfully.",
            Data = dto, ServerTime = DateTimeOffset.UtcNow
        });
    }

    /// <summary>Deactivates a shift template. Fails if active assignments exist.</summary>
    [HttpDelete("templates/{id:long}")]
    [HasPermission(Permissions.ManageShiftTemplate)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeactivateTemplate(long id, CancellationToken ct)
    {
        await _templateService.DeactivateAsync(id, ct);
        return NoContent();
    }

    // ─────────────────────────────────────────────────────────────
    //  ASSIGNMENTS
    // ─────────────────────────────────────────────────────────────

    /// <summary>Lists shift assignments with filtering and paging.</summary>
    [HttpGet("assignments")]
    [HasPermission(Permissions.ViewShift)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ShiftAssignmentListDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAssignments([FromQuery] GetShiftAssignmentRequest request, CancellationToken ct)
    {
        var (items, totalCount) = await _assignmentService.GetAssignmentsAsync(request, ct);

        var pageIndex = request.PageIndex > 0 ? request.PageIndex : 1;
        var pageSize = request.PageSize > 0 ? request.PageSize : 20;

        return Ok(new ApiResponse<PagedResult<ShiftAssignmentListDto>>
        {
            Success = true, Code = 200,
            UserMessage = "Shift assignments retrieved successfully.",
            Data = new PagedResult<ShiftAssignmentListDto>
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

    /// <summary>Returns a live-board oriented view of shift attendance and current status.</summary>
    [HttpGet("live-board")]
    [HasPermission(Permissions.ViewShift)]
    [ProducesResponseType(typeof(ApiResponse<List<ShiftLiveBoardItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLiveBoard([FromQuery] GetShiftAssignmentRequest request, CancellationToken ct)
    {
        var items = await _assignmentService.GetLiveBoardAsync(request, ct);

        return Ok(new ApiResponse<List<ShiftLiveBoardItemDto>>
        {
            Success = true,
            Code = 200,
            UserMessage = "Shift live board retrieved successfully.",
            Data = items,
            ServerTime = DateTimeOffset.UtcNow
        });
    }

    /// <summary>Gets full detail for a single shift assignment including attendance.</summary>
    [HttpGet("assignments/{id:long}")]
    [HasPermission(Permissions.ViewShift)]
    [ProducesResponseType(typeof(ApiResponse<ShiftAssignmentDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAssignment(long id, CancellationToken ct)
    {
        var dto = await _assignmentService.GetByIdAsync(id, ct);
        return Ok(new ApiResponse<ShiftAssignmentDetailDto>
        {
            Success = true, Code = 200,
            UserMessage = "Shift assignment retrieved successfully.",
            Data = dto, ServerTime = DateTimeOffset.UtcNow
        });
    }

    /// <summary>Assigns a staff member to a shift template on a specific date.</summary>
    [HttpPost("assignments")]
    [HasPermission(Permissions.AssignShift)]
    [ProducesResponseType(typeof(ApiResponse<ShiftAssignmentDetailDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateAssignment([FromBody] CreateShiftAssignmentRequest request, CancellationToken ct)
    {
        var dto = await _assignmentService.CreateAssignmentAsync(request, GetStaffId(), ct);
        return StatusCode(StatusCodes.Status201Created, new ApiResponse<ShiftAssignmentDetailDto>
        {
            Success = true, Code = 201,
            UserMessage = "Shift assignment created successfully.",
            Data = dto, ServerTime = DateTimeOffset.UtcNow
        });
    }

    /// <summary>Updates planned times or notes on an assignment.</summary>
    [HttpPut("assignments/{id:long}")]
    [HasPermission(Permissions.AssignShift)]
    [ProducesResponseType(typeof(ApiResponse<ShiftAssignmentDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAssignment(long id, [FromBody] UpdateShiftAssignmentRequest request, CancellationToken ct)
    {
        var dto = await _assignmentService.UpdateAssignmentAsync(id, request, GetStaffId(), ct);
        return Ok(new ApiResponse<ShiftAssignmentDetailDto>
        {
            Success = true, Code = 200,
            UserMessage = "Shift assignment updated successfully.",
            Data = dto, ServerTime = DateTimeOffset.UtcNow
        });
    }

    /// <summary>Cancels an assignment. Fails if staff has already checked in.</summary>
    [HttpDelete("assignments/{id:long}")]
    [HasPermission(Permissions.AssignShift)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CancelAssignment(long id, CancellationToken ct)
    {
        await _assignmentService.CancelAssignmentAsync(id, ct);
        return NoContent();
    }

    /// <summary>Bulk-create assignments for multiple staff on a single shift/date.</summary>
    [HttpPost("assignments/bulk")]
    [HasPermission(Permissions.AssignShift)]
    [ProducesResponseType(typeof(ApiResponse<List<ShiftAssignmentDetailDto>>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> BulkCreateAssignment([FromBody] BulkCreateAssignmentRequest request, CancellationToken ct)
    {
        var dtos = await _assignmentService.BulkCreateAssignmentsAsync(request, GetStaffId(), ct);
        return StatusCode(StatusCodes.Status201Created, new ApiResponse<List<ShiftAssignmentDetailDto>>
        {
            Success = true, Code = 201,
            UserMessage = $"{dtos.Count} shift assignment(s) created successfully.",
            Data = dtos, ServerTime = DateTimeOffset.UtcNow
        });
    }

    /// <summary>Publishes DRAFT assignments → ASSIGNED, notifying staff members.</summary>
    [HttpPost("assignments/publish")]
    [HasPermission(Permissions.PublishShift)]
    [ProducesResponseType(typeof(ApiResponse<List<ShiftAssignmentListDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PublishAssignments([FromBody] PublishAssignmentsRequest request, CancellationToken ct)
    {
        var dtos = await _assignmentService.PublishAssignmentsAsync(request, GetStaffId(), ct);
        return Ok(new ApiResponse<List<ShiftAssignmentListDto>>
        {
            Success = true, Code = 200,
            UserMessage = $"{dtos.Count} assignment(s) published successfully.",
            Data = dtos, ServerTime = DateTimeOffset.UtcNow
        });
    }

    /// <summary>Copies all active assignments from source week to target week.</summary>
    [HttpPost("assignments/copy-week")]
    [HasPermission(Permissions.AssignShift)]
    [ProducesResponseType(typeof(ApiResponse<List<ShiftAssignmentListDto>>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CopyWeek([FromBody] CopyWeekRequest request, CancellationToken ct)
    {
        var dtos = await _assignmentService.CopyWeekAsync(request, GetStaffId(), ct);
        return StatusCode(StatusCodes.Status201Created, new ApiResponse<List<ShiftAssignmentListDto>>
        {
            Success = true, Code = 201,
            UserMessage = $"{dtos.Count} assignment(s) copied successfully.",
            Data = dtos, ServerTime = DateTimeOffset.UtcNow
        });
    }

    /// <summary>Reassigns a shift to a different staff member.</summary>
    [HttpPut("assignments/{id:long}/reassign")]
    [HasPermission(Permissions.AssignShift)]
    [ProducesResponseType(typeof(ApiResponse<ShiftAssignmentDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReassignAssignment(long id, [FromBody] ReassignRequest request, CancellationToken ct)
    {
        var dto = await _assignmentService.ReassignAsync(id, request, GetStaffId(), ct);
        return Ok(new ApiResponse<ShiftAssignmentDetailDto>
        {
            Success = true, Code = 200,
            UserMessage = "Shift reassigned successfully.",
            Data = dto, ServerTime = DateTimeOffset.UtcNow
        });
    }

    /// <summary>Staff confirms their own upcoming assigned shift.</summary>
    [HttpPost("assignments/{id:long}/confirm")]
    [HasPermission(Permissions.ViewOwnShift)]
    [ProducesResponseType(typeof(ApiResponse<ShiftAssignmentDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ConfirmAssignment(long id, CancellationToken ct)
    {
        var dto = await _assignmentService.ConfirmAssignmentAsync(id, GetStaffId(), ct);
        return Ok(new ApiResponse<ShiftAssignmentDetailDto>
        {
            Success = true, Code = 200,
            UserMessage = "Shift confirmed successfully.",
            Data = dto, ServerTime = DateTimeOffset.UtcNow
        });
    }

    /// <summary>Returns the team schedule (all staff × 7 days) for a given week.</summary>
    [HttpGet("team-schedule")]
    [HasPermission(Permissions.ViewShift)]
    [ProducesResponseType(typeof(ApiResponse<List<ShiftAssignmentListDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTeamSchedule([FromQuery] TeamScheduleRequest request, CancellationToken ct)
    {
        var data = await _assignmentService.GetTeamScheduleAsync(request, ct);
        return Ok(new ApiResponse<List<ShiftAssignmentListDto>>
        {
            Success = true, Code = 200,
            UserMessage = "Team schedule retrieved successfully.",
            Data = data, ServerTime = DateTimeOffset.UtcNow
        });
    }

    // ─────────────────────────────────────────────────────────────
    //  MY SHIFTS (staff-only view)
    // ─────────────────────────────────────────────────────────────

    /// <summary>Returns shifts assigned to the currently authenticated staff member with attendance details.</summary>
    [HttpGet("my-shifts")]
    [HasPermission(Permissions.ViewOwnShift)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ShiftAssignmentDetailDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyShifts([FromQuery] GetShiftAssignmentRequest request, CancellationToken ct)
    {
        var staffId = GetStaffId();
        var (items, totalCount) = await _assignmentService.GetMyShiftsAsync(staffId, request, ct);

        var pageIndex = request.PageIndex > 0 ? request.PageIndex : 1;
        var pageSize = request.PageSize > 0 ? request.PageSize : 20;

        return Ok(new ApiResponse<PagedResult<ShiftAssignmentDetailDto>>
        {
            Success = true, Code = 200,
            UserMessage = "Your shifts retrieved successfully.",
            Data = new PagedResult<ShiftAssignmentDetailDto>
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

    // ─────────────────────────────────────────────────────────────
    //  ATTENDANCE
    // ─────────────────────────────────────────────────────────────

    /// <summary>Staff check-in for an assigned shift.</summary>
    [HttpPost("assignments/{id:long}/check-in")]
    [HasPermission(Permissions.CheckInShift)]
    [ProducesResponseType(typeof(ApiResponse<AttendanceRecordDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CheckIn(long id, CancellationToken ct)
    {
        var dto = await _attendanceService.CheckInAsync(id, GetStaffId(), ct);
        return Ok(new ApiResponse<AttendanceRecordDto>
        {
            Success = true, Code = 200,
            UserMessage = "Checked in successfully.",
            Data = dto, ServerTime = DateTimeOffset.UtcNow
        });
    }

    /// <summary>Staff check-out for an assigned shift.</summary>
    [HttpPost("assignments/{id:long}/check-out")]
    [HasPermission(Permissions.CheckOutShift)]
    [ProducesResponseType(typeof(ApiResponse<AttendanceRecordDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CheckOut(long id, CancellationToken ct)
    {
        var dto = await _attendanceService.CheckOutAsync(id, GetStaffId(), ct);
        return Ok(new ApiResponse<AttendanceRecordDto>
        {
            Success = true, Code = 200,
            UserMessage = "Checked out successfully.",
            Data = dto, ServerTime = DateTimeOffset.UtcNow
        });
    }

    /// <summary>Manager-only attendance adjustment with required reason.</summary>
    [HttpPatch("attendance/{id:long}")]
    [HasPermission(Permissions.AdjustAttendance)]
    [ProducesResponseType(typeof(ApiResponse<AttendanceRecordDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AdjustAttendance(long id, [FromBody] AdjustAttendanceRequest request, CancellationToken ct)
    {
        var dto = await _attendanceService.AdjustAttendanceAsync(id, request, GetStaffId(), ct);
        return Ok(new ApiResponse<AttendanceRecordDto>
        {
            Success = true, Code = 200,
            UserMessage = "Attendance adjusted successfully.",
            Data = dto, ServerTime = DateTimeOffset.UtcNow
        });
    }

    // ─────────────────────────────────────────────────────────────
    //  REPORTS
    // ─────────────────────────────────────────────────────────────

    /// <summary>Top-level KPI snapshot for the reports dashboard.</summary>
    [HttpGet("reports/snapshot")]
    [HasPermission(Permissions.ViewShiftReport)]
    [ProducesResponseType(typeof(ApiResponse<ShiftReportSnapshotDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReportSnapshot(
        [FromQuery] DateOnly fromDate, [FromQuery] DateOnly toDate, CancellationToken ct)
    {
        var dto = await _attendanceService.GetReportSnapshotAsync(fromDate, toDate, ct);
        return Ok(new ApiResponse<ShiftReportSnapshotDto>
        {
            Success = true, Code = 200,
            UserMessage = "Report snapshot retrieved successfully.",
            Data = dto, ServerTime = DateTimeOffset.UtcNow
        });
    }

    /// <summary>Attendance report with paging and filters.</summary>
    [HttpGet("reports/attendance")]
    [HasPermission(Permissions.ViewShiftReport)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<AttendanceReportRowDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAttendanceReport([FromQuery] AttendanceReportRequest request, CancellationToken ct)
    {
        var (items, totalCount) = await _attendanceService.GetAttendanceReportAsync(request, ct);

        var pageIndex = request.PageIndex > 0 ? request.PageIndex : 1;
        var pageSize = request.PageSize > 0 ? request.PageSize : 20;

        return Ok(new ApiResponse<PagedResult<AttendanceReportRowDto>>
        {
            Success = true, Code = 200,
            UserMessage = "Attendance report retrieved successfully.",
            Data = new PagedResult<AttendanceReportRowDto>
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

    /// <summary>Worked-hours report: scheduled vs actual per staff.</summary>
    [HttpGet("reports/worked-hours")]
    [HasPermission(Permissions.ViewShiftReport)]
    [ProducesResponseType(typeof(ApiResponse<List<WorkedHoursReportRowDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWorkedHoursReport(
        [FromQuery] DateOnly fromDate, [FromQuery] DateOnly toDate,
        [FromQuery] long? staffId, CancellationToken ct)
    {
        var data = await _attendanceService.GetWorkedHoursReportAsync(fromDate, toDate, staffId, ct);
        return Ok(new ApiResponse<List<WorkedHoursReportRowDto>>
        {
            Success = true, Code = 200,
            UserMessage = "Worked-hours report retrieved successfully.",
            Data = data, ServerTime = DateTimeOffset.UtcNow
        });
    }

    /// <summary>Exceptions report: late arrivals, absences, early departures, manual adjustments.</summary>
    [HttpGet("reports/exceptions")]
    [HasPermission(Permissions.ViewShiftReport)]
    [ProducesResponseType(typeof(ApiResponse<List<AttendanceExceptionReportRowDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetExceptionsReport(
        [FromQuery] DateOnly fromDate, [FromQuery] DateOnly toDate,
        [FromQuery] long? staffId, CancellationToken ct)
    {
        var data = await _attendanceService.GetExceptionsReportAsync(fromDate, toDate, staffId, ct);
        return Ok(new ApiResponse<List<AttendanceExceptionReportRowDto>>
        {
            Success = true, Code = 200,
            UserMessage = "Exceptions report retrieved successfully.",
            Data = data, ServerTime = DateTimeOffset.UtcNow
        });
    }
}
