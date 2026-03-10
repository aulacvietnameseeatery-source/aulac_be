using API.Models;
using Core.Attribute;
using Core.Data;
using Core.DTO.Shift;
using Core.Interface.Service.Shift;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers;

/// <summary>
/// Manages shift schedules, staff assignments, attendance tracking, live board, and reports.
/// </summary>
[ApiController]
[Route("api/shifts")]
[Authorize]
public class ShiftController : ControllerBase
{
    private readonly IShiftScheduleService _scheduleService;
    private readonly IShiftAssignmentService _assignmentService;
    private readonly IAttendanceService _attendanceService;

    public ShiftController(
        IShiftScheduleService scheduleService,
           IShiftAssignmentService assignmentService,
       IAttendanceService attendanceService)
    {
        _scheduleService = scheduleService;
        _assignmentService = assignmentService;
        _attendanceService = attendanceService;
    }

    private long GetStaffId() =>
  long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
    ?? throw new UnauthorizedAccessException("Staff ID not found in token"));

    // 
    //  SCHEDULES
    // 

    /// <summary>Lists shift schedules with filtering and paging.</summary>
    [HttpGet("schedules")]
    [HasPermission(Permissions.ViewShift)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ShiftScheduleListDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSchedules([FromQuery] GetShiftScheduleRequest request, CancellationToken ct)
    {
        var (items, totalCount) = await _scheduleService.GetSchedulesAsync(request, ct);

        var pageIndex = request.PageIndex > 0 ? request.PageIndex : 1;
        var pageSize = request.PageSize > 0 ? request.PageSize : 20;

        return Ok(new ApiResponse<PagedResult<ShiftScheduleListDto>>
        {
            Success = true,
            Code = 200,
            UserMessage = "Shift schedules retrieved successfully.",
            Data = new PagedResult<ShiftScheduleListDto>
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

    /// <summary>Gets full detail for a shift schedule including assignments and attendance.</summary>
    [HttpGet("schedules/{id:long}")]
    [HasPermission(Permissions.ViewShift)]
    [ProducesResponseType(typeof(ApiResponse<ShiftScheduleDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSchedule(long id, CancellationToken ct)
    {
        var dto = await _scheduleService.GetScheduleByIdAsync(id, ct);
        return Ok(new ApiResponse<ShiftScheduleDetailDto>
        {
            Success = true,
            Code = 200,
            UserMessage = "Shift schedule retrieved successfully.",
            Data = dto,
            ServerTime = DateTimeOffset.UtcNow
        });
    }

    /// <summary>Creates a new shift schedule (initially DRAFT).</summary>
    [HttpPost("schedules")]
    [HasPermission(Permissions.ScheduleShift)]
    [ProducesResponseType(typeof(ApiResponse<ShiftScheduleDetailDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateSchedule([FromBody] CreateShiftScheduleRequest request, CancellationToken ct)
    {
        var dto = await _scheduleService.CreateScheduleAsync(request, GetStaffId(), ct);
        return StatusCode(StatusCodes.Status201Created, new ApiResponse<ShiftScheduleDetailDto>
        {
            Success = true,
            Code = 201,
            UserMessage = "Shift schedule created successfully.",
            Data = dto,
            ServerTime = DateTimeOffset.UtcNow
        });
    }

    /// <summary>Updates a shift schedule (times, status, or notes).</summary>
    [HttpPut("schedules/{id:long}")]
    [HasPermission(Permissions.ScheduleShift)]
    [ProducesResponseType(typeof(ApiResponse<ShiftScheduleDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSchedule(long id, [FromBody] UpdateShiftScheduleRequest request, CancellationToken ct)
    {
        var dto = await _scheduleService.UpdateScheduleAsync(id, request, GetStaffId(), ct);
        return Ok(new ApiResponse<ShiftScheduleDetailDto>
        {
            Success = true,
            Code = 200,
            UserMessage = "Shift schedule updated successfully.",
            Data = dto,
            ServerTime = DateTimeOffset.UtcNow
        });
    }

    // 
    //  ASSIGNMENTS
    // 

    /// <summary>Lists shift assignments with filtering and paging.</summary>
    [HttpGet("assignments")]
    [HasPermission(Permissions.ViewShift)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ShiftAssignmentDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAssignments([FromQuery] GetShiftAssignmentRequest request, CancellationToken ct)
    {
        var (items, totalCount) = await _assignmentService.GetAssignmentsAsync(request, ct);

        var pageIndex = request.PageIndex > 0 ? request.PageIndex : 1;
        var pageSize = request.PageSize > 0 ? request.PageSize : 20;

        return Ok(new ApiResponse<PagedResult<ShiftAssignmentDto>>
        {
            Success = true,
            Code = 200,
            UserMessage = "Shift assignments retrieved successfully.",
            Data = new PagedResult<ShiftAssignmentDto>
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

    /// <summary>Creates one or many staff assignments for a shift schedule.</summary>
    [HttpPost("assignments")]
    [HasPermission(Permissions.AssignShift)]
    [ProducesResponseType(typeof(ApiResponse<List<ShiftAssignmentDto>>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateAssignments([FromBody] CreateShiftAssignmentRequest request, CancellationToken ct)
    {
        var dtos = await _assignmentService.CreateAssignmentsAsync(request, GetStaffId(), ct);
        return StatusCode(StatusCodes.Status201Created, new ApiResponse<List<ShiftAssignmentDto>>
        {
            Success = true,
            Code = 201,
            UserMessage = $"{dtos.Count} assignment(s) created successfully.",
            Data = dtos,
            ServerTime = DateTimeOffset.UtcNow
        });
    }

    /// <summary>Soft-cancels an assignment if attendance hasn't started.</summary>
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

    // 
    //  ATTENDANCE
    // 

    /// <summary>Staff check-in for an assigned shift.</summary>
    [HttpPost("assignments/{id:long}/check-in")]
    [HasPermission(Permissions.CheckInShift)]
    [ProducesResponseType(typeof(ApiResponse<AttendanceRecordDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CheckIn(long id, CancellationToken ct)
    {
        var dto = await _attendanceService.CheckInAsync(id, ct);
        return Ok(new ApiResponse<AttendanceRecordDto>
        {
            Success = true,
            Code = 200,
            UserMessage = "Checked in successfully.",
            Data = dto,
            ServerTime = DateTimeOffset.UtcNow
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
        var dto = await _attendanceService.CheckOutAsync(id, ct);
        return Ok(new ApiResponse<AttendanceRecordDto>
        {
            Success = true,
            Code = 200,
            UserMessage = "Checked out successfully.",
            Data = dto,
            ServerTime = DateTimeOffset.UtcNow
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
            Success = true,
            Code = 200,
            UserMessage = "Attendance adjusted successfully.",
            Data = dto,
            ServerTime = DateTimeOffset.UtcNow
        });
    }

    // 
    //  LIVE BOARD
    // 

    /// <summary>Returns the live on-duty board for the current (or specified) business date.</summary>
    [HttpGet("live")]
    [HasPermission(Permissions.ViewShift)]
    [ProducesResponseType(typeof(ApiResponse<LiveShiftBoardDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLiveBoard([FromQuery] DateOnly? businessDate, CancellationToken ct)
    {
        var dto = await _attendanceService.GetLiveBoardAsync(businessDate, ct);
        return Ok(new ApiResponse<LiveShiftBoardDto>
        {
            Success = true,
            Code = 200,
            UserMessage = "Live board data retrieved successfully.",
            Data = dto,
            ServerTime = DateTimeOffset.UtcNow
        });
    }

    // 
    //  REPORTS
    // 

    /// <summary>Attendance report with paging and filters.</summary>
    [HttpGet("reports/attendance")]
    [HasPermission(Permissions.ViewShiftReport)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ShiftAssignmentDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAttendanceReport([FromQuery] AttendanceReportRequest request, CancellationToken ct)
    {
        var (items, totalCount) = await _attendanceService.GetAttendanceReportAsync(request, ct);

        var pageIndex = request.PageIndex > 0 ? request.PageIndex : 1;
        var pageSize = request.PageSize > 0 ? request.PageSize : 20;

        return Ok(new ApiResponse<PagedResult<ShiftAssignmentDto>>
        {
            Success = true,
            Code = 200,
            UserMessage = "Attendance report retrieved successfully.",
            Data = new PagedResult<ShiftAssignmentDto>
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

    /// <summary>Worked-hours report: scheduled vs actual.</summary>
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
            Success = true,
            Code = 200,
            UserMessage = "Worked-hours report retrieved successfully.",
            Data = data,
            ServerTime = DateTimeOffset.UtcNow
        });
    }

    /// <summary>Exceptions report: late, absent, early leave, manual adjustments.</summary>
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
            Success = true,
            Code = 200,
            UserMessage = "Exceptions report retrieved successfully.",
            Data = data,
            ServerTime = DateTimeOffset.UtcNow
        });
    }
}
