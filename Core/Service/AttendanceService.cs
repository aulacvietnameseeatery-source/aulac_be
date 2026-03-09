using Core.Data;
using Core.DTO.Shift;
using Core.Entity;
using Core.Exceptions;
using Core.Extensions;
using Core.Interface.Repo;
using Core.Interface.Service.Entity;
using Core.Interface.Service.Shift;
using Microsoft.Extensions.Options;
using LookupType = Core.Enum.LookupType;

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

    #region ?? Check-in / Check-out ??

    public async Task<AttendanceRecordDto> CheckInAsync(long assignmentId, CancellationToken ct = default)
    {
        var assignment = await _assignmentRepo.GetByIdAsync(assignmentId, ct)
     ?? throw new NotFoundException("Shift assignment not found");

        // Guard: assignment must not be cancelled
        var assignmentStatus = assignment.AssignmentStatusLv?.ValueCode ?? "UNKNOWN";
        if (assignmentStatus == nameof(ShiftAssignmentStatusCode.CANCELLED))
            throw new ValidationException("Cannot check in for a cancelled assignment");

        // Guard: schedule must be published
        var scheduleStatus = assignment.ShiftSchedule.StatusLv?.ValueCode ?? "UNKNOWN";
        if (scheduleStatus != nameof(ShiftStatusCode.PUBLISHED))
            throw new ValidationException($"Cannot check in — schedule status is '{scheduleStatus}'");

        // Guard: no duplicate check-in
        var existing = await _attendanceRepo.GetByAssignmentIdAsync(assignmentId, ct);
        if (existing?.ActualCheckInAt is not null)
            throw new ConflictException("Already checked in for this shift");

        var now = DateTime.UtcNow;
        var plannedStart = assignment.ShiftSchedule.PlannedStartAt;
        var plannedEnd = assignment.ShiftSchedule.PlannedEndAt;

        // Guard: not too early
        if (now < plannedStart.AddMinutes(-_options.AllowedEarlyCheckInMinutes))
            throw new ValidationException(
        $"Too early to check in. Earliest allowed: {plannedStart.AddMinutes(-_options.AllowedEarlyCheckInMinutes):HH:mm}");

        // Guard: not after shift ended
        if (now > plannedEnd)
            throw new ValidationException("Shift has already ended, cannot check in");

        // ?? Compute late status ??
        var isLate = now > plannedStart.AddMinutes(_options.LateGraceMinutes);
        var lateMinutes = isLate ? (int)(now - plannedStart).TotalMinutes : 0;

        AttendanceStatusCode status = isLate ? AttendanceStatusCode.LATE : AttendanceStatusCode.ACTIVE;
        var statusLvId = await status.ToAttendanceStatusIdAsync(_lookupResolver, ct);

        if (existing is not null)
        {
            // Update the pre-created SCHEDULED record
            existing.ActualCheckInAt = now;
            existing.AttendanceStatusLvId = statusLvId;
            existing.LateMinutes = lateMinutes;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            // Create new attendance record
            var record = new AttendanceRecord
            {
                ShiftAssignmentId = assignmentId,
                AttendanceStatusLvId = statusLvId,
                ActualCheckInAt = now,
                LateMinutes = lateMinutes,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _attendanceRepo.Add(record);
        }

        await _unitOfWork.SaveChangesAsync(ct);

        var updated = await _attendanceRepo.GetByAssignmentIdAsync(assignmentId, ct)!;
        return ShiftScheduleService.MapAttendance(updated!);
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
        var plannedEnd = assignment!.ShiftSchedule.PlannedEndAt;

        // ?? Compute early-leave and worked minutes ??
        var isEarlyLeave = now < plannedEnd.AddMinutes(-_options.EarlyLeaveBufferMinutes);
        var earlyLeaveMinutes = isEarlyLeave ? (int)(plannedEnd - now).TotalMinutes : 0;
        var workedMinutes = Math.Max(0, (int)(now - record.ActualCheckInAt.Value).TotalMinutes);

        // Final status: keep LATE flag if was late, otherwise determine
        var currentCode = record.AttendanceStatusLv?.ValueCode;
        AttendanceStatusCode finalStatus;
        if (isEarlyLeave)
            finalStatus = AttendanceStatusCode.EARLY_LEAVE;
        else
            finalStatus = AttendanceStatusCode.COMPLETED;

        var statusLvId = await finalStatus.ToAttendanceStatusIdAsync(_lookupResolver, ct);

        record.ActualCheckOutAt = now;
        record.EarlyLeaveMinutes = earlyLeaveMinutes;
        record.WorkedMinutes = workedMinutes;
        record.AttendanceStatusLvId = statusLvId;
        record.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(ct);

        var updated = await _attendanceRepo.GetByAssignmentIdAsync(assignmentId, ct)!;
        return ShiftScheduleService.MapAttendance(updated!);
    }

    #endregion

    #region ?? Manager adjustment ??

    public async Task<AttendanceRecordDto> AdjustAttendanceAsync(
        long attendanceId, AdjustAttendanceRequest request, long reviewerStaffId, CancellationToken ct = default)
    {
        var record = await _attendanceRepo.GetByIdAsync(attendanceId, ct)
      ?? throw new NotFoundException("Attendance record not found");

        if (string.IsNullOrWhiteSpace(request.AdjustmentReason))
            throw new ValidationException("Adjustment reason is required");

        var schedule = record.ShiftAssignment.ShiftSchedule;

        if (request.ActualCheckInAt.HasValue)
        {
            record.ActualCheckInAt = request.ActualCheckInAt.Value;

            // Recompute late
            var isLate = request.ActualCheckInAt.Value > schedule.PlannedStartAt.AddMinutes(_options.LateGraceMinutes);
            record.LateMinutes = isLate ? (int)(request.ActualCheckInAt.Value - schedule.PlannedStartAt).TotalMinutes : 0;
        }

        if (request.ActualCheckOutAt.HasValue)
        {
            var checkIn = record.ActualCheckInAt
          ?? throw new ValidationException("Cannot set check-out without a check-in time");

            if (request.ActualCheckOutAt.Value <= checkIn)
                throw new ValidationException("Check-out must be after check-in");

            record.ActualCheckOutAt = request.ActualCheckOutAt.Value;

            // Recompute early-leave and worked
            var isEarlyLeave = request.ActualCheckOutAt.Value < schedule.PlannedEndAt.AddMinutes(-_options.EarlyLeaveBufferMinutes);
            record.EarlyLeaveMinutes = isEarlyLeave
     ? (int)(schedule.PlannedEndAt - request.ActualCheckOutAt.Value).TotalMinutes : 0;
            record.WorkedMinutes = Math.Max(0, (int)(request.ActualCheckOutAt.Value - checkIn).TotalMinutes);

            // Determine final status
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
        return ShiftScheduleService.MapAttendance(updated!);
    }

    #endregion

    #region ?? Live Board ??

    public async Task<LiveShiftBoardDto> GetLiveBoardAsync(DateOnly? businessDate = null, CancellationToken ct = default)
    {
        var date = businessDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var assignments = await _attendanceRepo.GetLiveBoardDataAsync(date, ct);

        var summary = new LiveBoardSummary();
        var rows = new List<LiveShiftBoardRowDto>();

        foreach (var a in assignments)
        {
            var attendanceCode = a.AttendanceRecord?.AttendanceStatusLv?.ValueCode;

            // Count by status
            switch (attendanceCode)
            {
                case nameof(AttendanceStatusCode.ACTIVE): summary.Active++; break;
                case nameof(AttendanceStatusCode.LATE): summary.Late++; break;
                case nameof(AttendanceStatusCode.ABSENT): summary.Absent++; break;
                case nameof(AttendanceStatusCode.COMPLETED):
                case nameof(AttendanceStatusCode.EARLY_LEAVE): summary.Completed++; break;
                default: summary.Scheduled++; break;
            }

            rows.Add(new LiveShiftBoardRowDto
            {
                ShiftAssignmentId = a.ShiftAssignmentId,
                StaffId = a.StaffId,
                StaffName = a.Staff?.FullName ?? "Unknown",
                RoleName = a.Role?.RoleName ?? "Unknown",
                ShiftTypeCode = a.ShiftSchedule.ShiftTypeLv?.ValueCode ?? "UNKNOWN",
                PlannedStartAt = a.ShiftSchedule.PlannedStartAt,
                PlannedEndAt = a.ShiftSchedule.PlannedEndAt,
                ActualCheckInAt = a.AttendanceRecord?.ActualCheckInAt,
                ActualCheckOutAt = a.AttendanceRecord?.ActualCheckOutAt,
                AttendanceStatusCode = attendanceCode ?? nameof(AttendanceStatusCode.SCHEDULED),
                LateMinutes = a.AttendanceRecord?.LateMinutes ?? 0
            });
        }

        // Sort: LATE ? ABSENT ? ACTIVE ? SCHEDULED ? COMPLETED
        var statusPriority = new Dictionary<string, int>
        {
            [nameof(AttendanceStatusCode.LATE)] = 0,
            [nameof(AttendanceStatusCode.ABSENT)] = 1,
            [nameof(AttendanceStatusCode.ACTIVE)] = 2,
            [nameof(AttendanceStatusCode.SCHEDULED)] = 3,
            [nameof(AttendanceStatusCode.COMPLETED)] = 4,
            [nameof(AttendanceStatusCode.EARLY_LEAVE)] = 4,
            [nameof(AttendanceStatusCode.EXCUSED)] = 5,
        };

        rows = rows.OrderBy(r => statusPriority.GetValueOrDefault(r.AttendanceStatusCode, 99))
                    .ThenBy(r => r.PlannedStartAt)
                    .ToList();

        return new LiveShiftBoardDto
        {
            BusinessDate = date,
            Summary = summary,
            Rows = rows
        };
    }

    #endregion

    #region ?? Reports ??

    public async Task<(List<ShiftAssignmentDto> Items, int TotalCount)> GetAttendanceReportAsync(
     AttendanceReportRequest request, CancellationToken ct = default)
    {
        var (items, totalCount) = await _attendanceRepo.GetAttendanceReportAsync(request, ct);
        return (items.Select(ShiftScheduleService.MapAssignment).ToList(), totalCount);
    }

    public async Task<List<AttendanceExceptionReportRowDto>> GetExceptionsReportAsync(
     DateOnly fromDate, DateOnly toDate, long? staffId, CancellationToken ct = default)
    {
        var data = await _attendanceRepo.GetExceptionDataAsync(fromDate, toDate, staffId, ct);

        return data.Select(a =>
        {
            var ar = a.AttendanceRecord!;
            var code = ar.AttendanceStatusLv?.ValueCode ?? "UNKNOWN";

            string exceptionType = code switch
            {
                nameof(AttendanceStatusCode.LATE) => "Late Arrival",
                nameof(AttendanceStatusCode.ABSENT) => "Absent",
                nameof(AttendanceStatusCode.EARLY_LEAVE) => "Early Departure",
                _ => ar.IsManualAdjustment ? "Manual Adjustment" : code
            };

            int minutesAffected = code switch
            {
                nameof(AttendanceStatusCode.LATE) => ar.LateMinutes,
                nameof(AttendanceStatusCode.EARLY_LEAVE) => ar.EarlyLeaveMinutes,
                _ => 0
            };

            return new AttendanceExceptionReportRowDto
            {
                StaffId = a.StaffId,
                StaffName = a.Staff?.FullName ?? "Unknown",
                RoleName = a.Role?.RoleName ?? "Unknown",
                BusinessDate = a.ShiftSchedule.BusinessDate,
                ShiftTypeCode = a.ShiftSchedule.ShiftTypeLv?.ValueCode ?? "UNKNOWN",
                ExceptionType = exceptionType,
                MinutesAffected = minutesAffected,
                IsManualAdjustment = ar.IsManualAdjustment,
                ReviewerName = ar.ReviewedByStaff?.FullName
            };
        }).ToList();
    }

    public async Task<List<WorkedHoursReportRowDto>> GetWorkedHoursReportAsync(DateOnly fromDate, DateOnly toDate, long? staffId, CancellationToken ct = default)
    {
        var data = await _attendanceRepo.GetWorkedHoursDataAsync(fromDate, toDate, staffId, ct);

        return data
            .GroupBy(a => new { a.StaffId, StaffName = a.Staff?.FullName ?? "Unknown" })
            .Select(g =>
            {
                var assignments = g.ToList();
                var scheduledMinutes = assignments.Sum(a =>(int)(a.ShiftSchedule.PlannedEndAt - a.ShiftSchedule.PlannedStartAt).TotalMinutes);
                var workedMinutes = assignments.Where(a => a.AttendanceRecord is not null).Sum(a => a.AttendanceRecord!.WorkedMinutes);
                var incomplete = assignments.Count(a =>a.AttendanceRecord is null || a.AttendanceRecord.ActualCheckOutAt is null);

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

    #endregion
}
