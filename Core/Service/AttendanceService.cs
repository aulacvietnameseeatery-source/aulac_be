using Core.Data;
using Core.DTO.Shift;
using Core.Entity;
using Core.Exceptions;
using Core.Extensions;
using Core.Interface.Repo;
using Core.Interface.Service.Entity;
using Core.Interface.Service.Shift;
using Microsoft.Extensions.Options;

namespace Core.Service;

public class AttendanceService : IAttendanceService
{
    private readonly IAttendanceRepository _attendanceRepo;
    private readonly IShiftAssignmentRepository _assignmentRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILookupResolver _lookupResolver;
    private readonly AttendanceOptions _options;

    public AttendanceService(
        IAttendanceRepository attendanceRepo,
        IShiftAssignmentRepository assignmentRepo,
        IUnitOfWork unitOfWork,
        ILookupResolver lookupResolver,
        IOptions<AttendanceOptions> options)
    {
        _attendanceRepo = attendanceRepo;
        _assignmentRepo = assignmentRepo;
        _unitOfWork = unitOfWork;
        _lookupResolver = lookupResolver;
        _options = options.Value;
    }

    #region Check-in / Check-out

    public async Task<AttendanceRecordDto> CheckInAsync(long assignmentId, CancellationToken ct = default)
    {
        var assignment = await _assignmentRepo.GetByIdWithDetailsAsync(assignmentId, ct)
            ?? throw new NotFoundException("Shift assignment not found");

        if (!assignment.IsActive)
            throw new ValidationException("Cannot check in for a cancelled assignment");

        var existing = await _attendanceRepo.GetByAssignmentIdAsync(assignmentId, ct);
        if (existing?.ActualCheckInAt is not null)
            throw new ConflictException("Already checked in for this shift");

        var now = DateTime.UtcNow;
        var plannedStart = assignment.PlannedStartAt;
        var plannedEnd = assignment.PlannedEndAt;

        if (now < plannedStart.AddMinutes(-_options.AllowedEarlyCheckInMinutes))
            throw new ValidationException(
                $"Too early to check in. Earliest allowed: {plannedStart.AddMinutes(-_options.AllowedEarlyCheckInMinutes):HH:mm}");

        if (now > plannedEnd)
            throw new ValidationException("Shift has already ended, cannot check in");

        var isLate = now > plannedStart.AddMinutes(_options.LateGraceMinutes);
        var lateMinutes = isLate ? (int)(now - plannedStart).TotalMinutes : 0;

        AttendanceStatusCode status = isLate ? AttendanceStatusCode.LATE : AttendanceStatusCode.ACTIVE;
        var statusLvId = await status.ToAttendanceStatusIdAsync(_lookupResolver, ct);

        if (existing is not null)
        {
            existing.ActualCheckInAt = now;
            existing.AttendanceStatusLvId = statusLvId;
            existing.LateMinutes = lateMinutes;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _attendanceRepo.Add(new AttendanceRecord
            {
                ShiftAssignmentId = assignmentId,
                AttendanceStatusLvId = statusLvId,
                ActualCheckInAt = now,
                LateMinutes = lateMinutes,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        await _unitOfWork.SaveChangesAsync(ct);

        var updated = await _attendanceRepo.GetByAssignmentIdAsync(assignmentId, ct)!;
        return ShiftAssignmentService.MapAttendance(updated!);
    }

    public async Task<AttendanceRecordDto> CheckOutAsync(long assignmentId, CancellationToken ct = default)
    {
        var record = await _attendanceRepo.GetByAssignmentIdAsync(assignmentId, ct)
            ?? throw new NotFoundException("No attendance record found — did you check in?");

        if (record.ActualCheckInAt is null)
            throw new ValidationException("Cannot check out before checking in");

        if (record.ActualCheckOutAt is not null)
            throw new ConflictException("Already checked out for this shift");

        var assignment = await _assignmentRepo.GetByIdAsync(assignmentId, ct)!;
        var now = DateTime.UtcNow;
        var plannedEnd = assignment!.PlannedEndAt;

        var isEarlyLeave = now < plannedEnd.AddMinutes(-_options.EarlyLeaveBufferMinutes);
        var earlyLeaveMinutes = isEarlyLeave ? (int)(plannedEnd - now).TotalMinutes : 0;
        var workedMinutes = Math.Max(0, (int)(now - record.ActualCheckInAt.Value).TotalMinutes);

        AttendanceStatusCode finalStatus = isEarlyLeave
            ? AttendanceStatusCode.EARLY_LEAVE
            : AttendanceStatusCode.COMPLETED;

        record.ActualCheckOutAt = now;
        record.EarlyLeaveMinutes = earlyLeaveMinutes;
        record.WorkedMinutes = workedMinutes;
        record.AttendanceStatusLvId = await finalStatus.ToAttendanceStatusIdAsync(_lookupResolver, ct);
        record.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(ct);

        var updated = await _attendanceRepo.GetByAssignmentIdAsync(assignmentId, ct)!;
        return ShiftAssignmentService.MapAttendance(updated!);
    }

    #endregion

    #region Manager adjustment

    public async Task<AttendanceRecordDto> AdjustAttendanceAsync(
        long attendanceId, AdjustAttendanceRequest request, long reviewerStaffId, CancellationToken ct = default)
    {
        var record = await _attendanceRepo.GetByIdWithDetailsAsync(attendanceId, ct)
            ?? throw new NotFoundException("Attendance record not found");

        if (string.IsNullOrWhiteSpace(request.AdjustmentReason))
            throw new ValidationException("Adjustment reason is required");

        var assignment = record.ShiftAssignment;

        if (request.ActualCheckInAt.HasValue)
        {
            record.ActualCheckInAt = request.ActualCheckInAt.Value;

            var isLate = request.ActualCheckInAt.Value > assignment.PlannedStartAt.AddMinutes(_options.LateGraceMinutes);
            record.LateMinutes = isLate
                ? (int)(request.ActualCheckInAt.Value - assignment.PlannedStartAt).TotalMinutes : 0;
        }

        if (request.ActualCheckOutAt.HasValue)
        {
            var checkIn = record.ActualCheckInAt
                ?? throw new ValidationException("Cannot set check-out without a check-in time");

            if (request.ActualCheckOutAt.Value <= checkIn)
                throw new ValidationException("Check-out must be after check-in");

            record.ActualCheckOutAt = request.ActualCheckOutAt.Value;

            var isEarlyLeave = request.ActualCheckOutAt.Value
                < assignment.PlannedEndAt.AddMinutes(-_options.EarlyLeaveBufferMinutes);

            record.EarlyLeaveMinutes = isEarlyLeave
                ? (int)(assignment.PlannedEndAt - request.ActualCheckOutAt.Value).TotalMinutes : 0;
            record.WorkedMinutes = Math.Max(0, (int)(request.ActualCheckOutAt.Value - checkIn).TotalMinutes);

            AttendanceStatusCode finalStatus;
            if (isEarlyLeave) finalStatus = AttendanceStatusCode.EARLY_LEAVE;
            else if (record.LateMinutes > 0) finalStatus = AttendanceStatusCode.LATE;
            else finalStatus = AttendanceStatusCode.COMPLETED;

            record.AttendanceStatusLvId = await finalStatus.ToAttendanceStatusIdAsync(_lookupResolver, ct);
        }

        record.IsManualAdjustment = true;
        record.AdjustmentReason = request.AdjustmentReason.Trim();
        record.ReviewedBy = reviewerStaffId;
        record.ReviewedAt = DateTime.UtcNow;
        record.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(ct);

        var updated = await _attendanceRepo.GetByIdWithDetailsAsync(attendanceId, ct)!;
        return ShiftAssignmentService.MapAttendance(updated!);
    }

    #endregion

    #region Reports

    public async Task<(List<AttendanceReportRowDto> Items, int TotalCount)> GetAttendanceReportAsync(
        AttendanceReportRequest request, CancellationToken ct = default)
    {
        var (items, totalCount) = await _assignmentRepo.GetAttendanceReportAsync(request, ct);
        return (items.Select(MapToReportRow).ToList(), totalCount);
    }

    public async Task<List<AttendanceExceptionReportRowDto>> GetExceptionsReportAsync(
        DateOnly fromDate, DateOnly toDate, long? staffId, CancellationToken ct = default)
    {
        var data = await _assignmentRepo.GetExceptionDataAsync(fromDate, toDate, staffId, ct);

        return data.Select(a =>
        {
            var ar = a.AttendanceRecord!;
            var code = ar.AttendanceStatusLv?.ValueCode ?? "UNKNOWN";

            var exceptionType = code switch
            {
                nameof(AttendanceStatusCode.LATE) => "Late Arrival",
                nameof(AttendanceStatusCode.ABSENT) => "Absent",
                nameof(AttendanceStatusCode.EARLY_LEAVE) => "Early Departure",
                _ => ar.IsManualAdjustment ? "Manual Adjustment" : code
            };

            var minutesAffected = code switch
            {
                nameof(AttendanceStatusCode.LATE) => ar.LateMinutes,
                nameof(AttendanceStatusCode.EARLY_LEAVE) => ar.EarlyLeaveMinutes,
                _ => 0
            };

            return new AttendanceExceptionReportRowDto
            {
                StaffId = a.StaffId,
                StaffName = a.Staff?.FullName ?? "Unknown",
                WorkDate = a.WorkDate,
                TemplateName = a.ShiftTemplate?.TemplateName ?? "Unknown",
                ExceptionType = exceptionType,
                MinutesAffected = minutesAffected,
                IsManualAdjustment = ar.IsManualAdjustment,
                ReviewerName = ar.ReviewedByStaff?.FullName
            };
        }).ToList();
    }

    public async Task<List<WorkedHoursReportRowDto>> GetWorkedHoursReportAsync(
        DateOnly fromDate, DateOnly toDate, long? staffId, CancellationToken ct = default)
    {
        var data = await _assignmentRepo.GetWorkedHoursDataAsync(fromDate, toDate, staffId, ct);

        return data
            .GroupBy(a => new { a.StaffId, StaffName = a.Staff?.FullName ?? "Unknown" })
            .Select(g =>
            {
                var assignments = g.ToList();
                var scheduledMinutes = assignments.Sum(a =>
                    (int)(a.PlannedEndAt - a.PlannedStartAt).TotalMinutes);
                var workedMinutes = assignments
                    .Where(a => a.AttendanceRecord is not null)
                    .Sum(a => a.AttendanceRecord!.WorkedMinutes);
                var incomplete = assignments.Count(a =>
                    a.AttendanceRecord is null || a.AttendanceRecord.ActualCheckOutAt is null);

                return new WorkedHoursReportRowDto
                {
                    StaffId = g.Key.StaffId,
                    StaffName = g.Key.StaffName,
                    ScheduledMinutes = scheduledMinutes,
                    WorkedMinutes = workedMinutes,
                    VarianceMinutes = workedMinutes - scheduledMinutes,
                    IncompleteRecords = incomplete
                };
            })
            .OrderBy(r => r.StaffName)
            .ToList();
    }

    public async Task<ShiftReportSnapshotDto> GetReportSnapshotAsync(
        DateOnly fromDate, DateOnly toDate, CancellationToken ct = default)
    {
        // Attendance count: total attendance report rows in the date range
        var attendanceRequest = new AttendanceReportRequest { FromDate = fromDate, ToDate = toDate, PageSize = 1 };
        var (_, attendanceCount) = await _assignmentRepo.GetAttendanceReportAsync(attendanceRequest, ct);

        // Worked staff count: unique staff in worked-hours data
        var workedData = await _assignmentRepo.GetWorkedHoursDataAsync(fromDate, toDate, null, ct);
        var workedStaffCount = workedData.Select(a => a.StaffId).Distinct().Count();

        // Exception count
        var exceptions = await _assignmentRepo.GetExceptionDataAsync(fromDate, toDate, null, ct);

        return new ShiftReportSnapshotDto
        {
            AttendanceCount = attendanceCount,
            WorkedStaffCount = workedStaffCount,
            ExceptionCount = exceptions.Count
        };
    }

    #endregion

    private static AttendanceReportRowDto MapToReportRow(ShiftAssignment a) => new()
    {
        ShiftAssignmentId = a.ShiftAssignmentId,
        StaffId = a.StaffId,
        StaffName = a.Staff?.FullName ?? "Unknown",
        WorkDate = a.WorkDate,
        TemplateName = a.ShiftTemplate?.TemplateName ?? "Unknown",
        PlannedStartAt = a.PlannedStartAt,
        PlannedEndAt = a.PlannedEndAt,
        AttendanceStatusCode = a.AttendanceRecord?.AttendanceStatusLv?.ValueCode ?? "NO_RECORD",
        ActualCheckInAt = a.AttendanceRecord?.ActualCheckInAt,
        ActualCheckOutAt = a.AttendanceRecord?.ActualCheckOutAt,
        LateMinutes = a.AttendanceRecord?.LateMinutes ?? 0,
        EarlyLeaveMinutes = a.AttendanceRecord?.EarlyLeaveMinutes ?? 0,
        WorkedMinutes = a.AttendanceRecord?.WorkedMinutes ?? 0,
        IsManualAdjustment = a.AttendanceRecord?.IsManualAdjustment ?? false
    };
}
