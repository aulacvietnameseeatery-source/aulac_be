using Core.DTO.Shift;
using Core.Entity;
using Core.Exceptions;
using Core.Extensions;
using Core.Interface.Repo;
using Core.Interface.Service.Entity;
using Core.Interface.Service.Shift;
using LookupType = Core.Enum.LookupType;

namespace Core.Service;

public class ShiftScheduleService : IShiftScheduleService
{
    private readonly IShiftScheduleRepository _scheduleRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILookupResolver _lookupResolver;

    /// <summary>
    /// Allowed status transitions for shift schedules.
    /// DRAFT -> PUBLISHED, CANCELLED
    /// PUBLISHED -> CLOSED, CANCELLED
    /// </summary>
    private static readonly Dictionary<string, HashSet<string>> AllowedTransitions = new()
    {
        [nameof(ShiftStatusCode.DRAFT)] = [nameof(ShiftStatusCode.PUBLISHED), nameof(ShiftStatusCode.CANCELLED)],
        [nameof(ShiftStatusCode.PUBLISHED)] = [nameof(ShiftStatusCode.CLOSED), nameof(ShiftStatusCode.CANCELLED)],
    };

    public ShiftScheduleService(
      IShiftScheduleRepository scheduleRepo,
      IUnitOfWork unitOfWork,
      ILookupResolver lookupResolver)
    {
        _scheduleRepo = scheduleRepo;
        _unitOfWork = unitOfWork;
        _lookupResolver = lookupResolver;
    }

    public async Task<(List<ShiftScheduleListDto> Items, int TotalCount)> GetSchedulesAsync(
        GetShiftScheduleRequest request, CancellationToken ct = default)
    {
        var (items, totalCount) = await _scheduleRepo.GetSchedulesAsync(request, ct);
        return (items.Select(MapToListDto).ToList(), totalCount);
    }

    public async Task<ShiftScheduleDetailDto> GetScheduleByIdAsync(long id, CancellationToken ct = default)
    {
        var schedule = await _scheduleRepo.GetByIdWithDetailsAsync(id, ct)
        ?? throw new NotFoundException("Shift schedule not found");
        return MapToDetailDto(schedule);
    }

    public async Task<ShiftScheduleDetailDto> CreateScheduleAsync(
      CreateShiftScheduleRequest request, long createdByStaffId, CancellationToken ct = default)
    {
        // ?? Validate ??
        if (request.PlannedStartAt >= request.PlannedEndAt)
            throw new ValidationException("Planned start must be earlier than planned end");

        await ValidateLookupAsync(request.ShiftTypeLvId, (ushort)LookupType.ShiftType, "shift type", ct);

        if (await _scheduleRepo.HasOverlappingScheduleAsync(
          request.BusinessDate, request.ShiftTypeLvId, request.PlannedStartAt, request.PlannedEndAt, ct: ct))
            throw new ConflictException("An overlapping shift schedule of the same type already exists for this date");

        var draftStatusId = await ShiftStatusCode.DRAFT.ToShiftStatusIdAsync(_lookupResolver, ct);

        var entity = new ShiftSchedule
        {
            BusinessDate = request.BusinessDate,
            ShiftTypeLvId = request.ShiftTypeLvId,
            StatusLvId = draftStatusId,
            PlannedStartAt = request.PlannedStartAt,
            PlannedEndAt = request.PlannedEndAt,
            Notes = request.Notes,
            CreatedBy = createdByStaffId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _scheduleRepo.Add(entity);
        await _unitOfWork.SaveChangesAsync(ct);

        var saved = await _scheduleRepo.GetByIdWithDetailsAsync(entity.ShiftScheduleId, ct)
                        ?? throw new NotFoundException("Schedule not found after creation");
        return MapToDetailDto(saved);
    }

    public async Task<ShiftScheduleDetailDto> UpdateScheduleAsync(
        long id, UpdateShiftScheduleRequest request, long updatedByStaffId, CancellationToken ct = default)
    {
        var schedule = await _scheduleRepo.GetByIdAsync(id, ct)
                            ?? throw new NotFoundException("Shift schedule not found");

        var currentStatusCode = schedule.StatusLv?.ValueCode ?? "UNKNOWN";

        // Cannot edit closed/cancelled schedules
        if (currentStatusCode is nameof(ShiftStatusCode.CLOSED) or nameof(ShiftStatusCode.CANCELLED))
            throw new ValidationException($"Cannot edit a schedule with status '{currentStatusCode}'");

        // ?? Status transition ??
        if (request.StatusLvId.HasValue)
        {
            await ValidateLookupAsync(request.StatusLvId.Value, (ushort)LookupType.ShiftStatus, "shift status", ct);

            // Resolve the new code by querying each known code against the resolver
            var newStatusCode = await ResolveStatusCodeAsync(request.StatusLvId.Value, ct);

            if (AllowedTransitions.TryGetValue(currentStatusCode, out var allowed) && !allowed.Contains(newStatusCode))
                throw new ValidationException(
                    $"Invalid status transition: {currentStatusCode} ? {newStatusCode}. " +
                    $"Allowed from {currentStatusCode}: {string.Join(", ", allowed)}");

            schedule.StatusLvId = request.StatusLvId.Value;
        }

        // ?? Time changes ??
        var start = request.PlannedStartAt ?? schedule.PlannedStartAt;
        var end = request.PlannedEndAt ?? schedule.PlannedEndAt;

        if (start >= end)
            throw new ValidationException("Planned start must be earlier than planned end");

        if (request.PlannedStartAt.HasValue || request.PlannedEndAt.HasValue)
        {
            if (await _scheduleRepo.HasOverlappingScheduleAsync(schedule.BusinessDate, schedule.ShiftTypeLvId, start, end, excludeId: id, ct: ct))
                throw new ConflictException("An overlapping shift schedule of the same type already exists for this date");

            schedule.PlannedStartAt = start;
            schedule.PlannedEndAt = end;
        }

        if (request.Notes is not null)
            schedule.Notes = request.Notes;

        schedule.UpdatedBy = updatedByStaffId;
        schedule.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(ct);

        var updated = await _scheduleRepo.GetByIdWithDetailsAsync(id, ct)
                        ?? throw new NotFoundException("Schedule not found after update");
        return MapToDetailDto(updated);
    }

    #region ?? Private helpers ??

    /// <summary>
    /// Resolves a status LV ID back to its value code by checking each known ShiftStatusCode.
    /// </summary>
    private async Task<string> ResolveStatusCodeAsync(uint statusLvId, CancellationToken ct)
    {
        foreach (var code in System.Enum.GetValues<ShiftStatusCode>())
        {
            var id = await _lookupResolver.TryGetIdAsync((ushort)LookupType.ShiftStatus, code.ToString(), ct);
            if (id == statusLvId) return code.ToString();
        }
        return "UNKNOWN";
    }

    private static ShiftScheduleListDto MapToListDto(ShiftSchedule s) => new()
    {
        ShiftScheduleId = s.ShiftScheduleId,
        BusinessDate = s.BusinessDate,
        ShiftTypeLvId = s.ShiftTypeLvId,
        ShiftTypeCode = s.ShiftTypeLv?.ValueCode ?? "UNKNOWN",
        ShiftTypeName = s.ShiftTypeLv?.ValueName ?? "Unknown",
        StatusLvId = s.StatusLvId,
        StatusCode = s.StatusLv?.ValueCode ?? "UNKNOWN",
        StatusName = s.StatusLv?.ValueName ?? "Unknown",
        PlannedStartAt = s.PlannedStartAt,
        PlannedEndAt = s.PlannedEndAt,
        Notes = s.Notes,
        AssignmentCount = s.ShiftAssignments.Count,
        CreatedAt = s.CreatedAt
    };

    private static ShiftScheduleDetailDto MapToDetailDto(ShiftSchedule s)
    {
        return new ShiftScheduleDetailDto
        {
            ShiftScheduleId = s.ShiftScheduleId,
            BusinessDate = s.BusinessDate,
            ShiftTypeLvId = s.ShiftTypeLvId,
            ShiftTypeCode = s.ShiftTypeLv?.ValueCode ?? "UNKNOWN",
            ShiftTypeName = s.ShiftTypeLv?.ValueName ?? "Unknown",
            StatusLvId = s.StatusLvId,
            StatusCode = s.StatusLv?.ValueCode ?? "UNKNOWN",
            StatusName = s.StatusLv?.ValueName ?? "Unknown",
            PlannedStartAt = s.PlannedStartAt,
            PlannedEndAt = s.PlannedEndAt,
            Notes = s.Notes,
            AssignmentCount = s.ShiftAssignments.Count,
            CreatedAt = s.CreatedAt,
            CreatedByName = s.CreatedByStaff?.FullName ?? "Unknown",
            UpdatedByName = s.UpdatedByStaff?.FullName,
            UpdatedAt = s.UpdatedAt,
            Assignments = s.ShiftAssignments.Select(MapAssignment).ToList()
        };
    }

    internal static ShiftAssignmentDto MapAssignment(ShiftAssignment a) => new()
    {
        ShiftAssignmentId = a.ShiftAssignmentId,
        ShiftScheduleId = a.ShiftScheduleId,
        StaffId = a.StaffId,
        StaffName = a.Staff?.FullName ?? "Unknown",
        RoleId = a.RoleId,
        RoleName = a.Role?.RoleName ?? "Unknown",
        AssignmentStatusLvId = a.AssignmentStatusLvId,
        AssignmentStatusCode = a.AssignmentStatusLv?.ValueCode ?? "UNKNOWN",
        AssignmentStatusName = a.AssignmentStatusLv?.ValueName ?? "Unknown",
        Remarks = a.Remarks,
        AssignedAt = a.AssignedAt,
        AssignedByName = a.AssignedByStaff?.FullName ?? "Unknown",
        Attendance = a.AttendanceRecord is not null ? MapAttendance(a.AttendanceRecord) : null
    };

    internal static AttendanceRecordDto MapAttendance(AttendanceRecord ar) => new()
    {
        AttendanceId = ar.AttendanceId,
        ShiftAssignmentId = ar.ShiftAssignmentId,
        AttendanceStatusLvId = ar.AttendanceStatusLvId,
        AttendanceStatusCode = ar.AttendanceStatusLv?.ValueCode ?? "UNKNOWN",
        AttendanceStatusName = ar.AttendanceStatusLv?.ValueName ?? "Unknown",
        ActualCheckInAt = ar.ActualCheckInAt,
        ActualCheckOutAt = ar.ActualCheckOutAt,
        LateMinutes = ar.LateMinutes,
        EarlyLeaveMinutes = ar.EarlyLeaveMinutes,
        WorkedMinutes = ar.WorkedMinutes,
        IsManualAdjustment = ar.IsManualAdjustment,
        AdjustmentReason = ar.AdjustmentReason,
        ReviewedByName = ar.ReviewedByStaff?.FullName,
        ReviewedAt = ar.ReviewedAt
    };

    private async Task ValidateLookupAsync(uint lvId, ushort typeId, string label, CancellationToken ct)
    {
        if (!await _scheduleRepo.IsValidLookupAsync(lvId, typeId, ct))
            throw new ValidationException($"Invalid {label} (LvId={lvId})");
    }

    #endregion
}
