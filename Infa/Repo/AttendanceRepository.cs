using Core.DTO.Shift;
using Core.Entity;
using Core.Interface.Repo;
using Infa.Data;
using Microsoft.EntityFrameworkCore;

namespace Infa.Repo;

public class AttendanceRepository : IAttendanceRepository
{
    private readonly RestaurantMgmtContext _context;

    public AttendanceRepository(RestaurantMgmtContext context)
    {
        _context = context;
    }

    public void Add(AttendanceRecord entity) => _context.AttendanceRecords.Add(entity);

    public async Task<AttendanceRecord?> GetByAssignmentIdAsync(long shiftAssignmentId, CancellationToken ct = default)
    {
        return await _context.AttendanceRecords
            .Include(ar => ar.AttendanceStatusLv)
            .FirstOrDefaultAsync(ar => ar.ShiftAssignmentId == shiftAssignmentId, ct);
    }

    public async Task<AttendanceRecord?> GetByIdAsync(long attendanceId, CancellationToken ct = default)
    {
        return await _context.AttendanceRecords
            .Include(ar => ar.AttendanceStatusLv)
            .Include(ar => ar.ShiftAssignment)
                .ThenInclude(a => a.ShiftSchedule)
            .FirstOrDefaultAsync(ar => ar.AttendanceId == attendanceId, ct);
    }

    public async Task<AttendanceRecord?> GetByIdWithDetailsAsync(long attendanceId, CancellationToken ct = default)
    {
        return await _context.AttendanceRecords
            .Include(ar => ar.AttendanceStatusLv)
            .Include(ar => ar.ReviewedByStaff)
            .Include(ar => ar.ShiftAssignment)
                .ThenInclude(a => a.ShiftSchedule)
                .ThenInclude(s => s.ShiftTypeLv)
            .Include(ar => ar.ShiftAssignment)
                .ThenInclude(a => a.Staff)
            .Include(ar => ar.ShiftAssignment)
                .ThenInclude(a => a.Role)
            .FirstOrDefaultAsync(ar => ar.AttendanceId == attendanceId, ct);
    }

    public async Task<List<ShiftAssignment>> GetLiveBoardDataAsync(DateOnly businessDate, CancellationToken ct = default)
    {
        return await _context.ShiftAssignments
            .Include(a => a.AssignmentStatusLv)
            .Include(a => a.ShiftSchedule)
                .ThenInclude(s => s.ShiftTypeLv)
            .Include(a => a.ShiftSchedule)
                .ThenInclude(s => s.StatusLv)
            .Include(a => a.Staff)
            .Include(a => a.Role)
            .Include(a => a.AttendanceRecord)
                .ThenInclude(ar => ar!.AttendanceStatusLv)
            .AsNoTracking()
            .Where(a => a.ShiftSchedule.BusinessDate == businessDate
                    && a.AssignmentStatusLv.ValueCode != nameof(ShiftAssignmentStatusCode.CANCELLED)
                    && a.ShiftSchedule.StatusLv.ValueCode != nameof(ShiftStatusCode.CANCELLED))
            .OrderBy(a => a.ShiftSchedule.PlannedStartAt)
                .ThenBy(a => a.Staff.FullName)
            .ToListAsync(ct);
    }

    public async Task<(List<ShiftAssignment> Items, int TotalCount)> GetAttendanceReportAsync(
    AttendanceReportRequest request, CancellationToken ct = default)
    {
        var query = _context.ShiftAssignments
            .Include(a => a.AssignmentStatusLv)
            .Include(a => a.ShiftSchedule)
                .ThenInclude(s => s.ShiftTypeLv)
            .Include(a => a.Staff)
            .Include(a => a.Role)
            .Include(a => a.AttendanceRecord)
                .ThenInclude(ar => ar!.AttendanceStatusLv)
            .Include(a => a.AttendanceRecord)
                .ThenInclude(ar => ar!.ReviewedByStaff)
            .AsNoTracking()
            .Where(a => a.AssignmentStatusLv.ValueCode != nameof(ShiftAssignmentStatusCode.CANCELLED))
            .AsQueryable();

        if (request.FromDate.HasValue)
            query = query.Where(a => a.ShiftSchedule.BusinessDate >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            query = query.Where(a => a.ShiftSchedule.BusinessDate <= request.ToDate.Value);

        if (request.StaffId.HasValue)
            query = query.Where(a => a.StaffId == request.StaffId.Value);

        if (request.ShiftTypeLvId.HasValue)
            query = query.Where(a => a.ShiftSchedule.ShiftTypeLvId == request.ShiftTypeLvId.Value);

        if (request.AttendanceStatusLvId.HasValue)
            query = query.Where(a => a.AttendanceRecord != null
                                && a.AttendanceRecord.AttendanceStatusLvId == request.AttendanceStatusLvId.Value);

        var totalCount = await query.CountAsync(ct);

        var pageIndex = request.PageIndex < 1 ? 1 : request.PageIndex;
        var pageSize = request.PageSize < 1 ? 20 : request.PageSize;

        var items = await query
            .OrderByDescending(a => a.ShiftSchedule.BusinessDate)
                .ThenBy(a => a.Staff.FullName)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<List<ShiftAssignment>> GetWorkedHoursDataAsync(
     DateOnly fromDate, DateOnly toDate, long? staffId, CancellationToken ct = default)
    {
        var query = _context.ShiftAssignments
            .Include(a => a.ShiftSchedule)
            .Include(a => a.Staff)
            .Include(a => a.AttendanceRecord)
            .AsNoTracking()
            .Where(a => a.AssignmentStatusLv.ValueCode != nameof(ShiftAssignmentStatusCode.CANCELLED)
                    && a.ShiftSchedule.BusinessDate >= fromDate
                    && a.ShiftSchedule.BusinessDate <= toDate)
            .AsQueryable();

        if (staffId.HasValue)
            query = query.Where(a => a.StaffId == staffId.Value);

        return await query.ToListAsync(ct);
    }

    public async Task<List<ShiftAssignment>> GetExceptionDataAsync(
      DateOnly fromDate, DateOnly toDate, long? staffId, CancellationToken ct = default)
    {
        var query = _context.ShiftAssignments
            .Include(a => a.ShiftSchedule)
                .ThenInclude(s => s.ShiftTypeLv)
            .Include(a => a.Staff)
            .Include(a => a.Role)
            .Include(a => a.AttendanceRecord)
                .ThenInclude(ar => ar!.AttendanceStatusLv)
            .Include(a => a.AttendanceRecord)
                .ThenInclude(ar => ar!.ReviewedByStaff)
            .AsNoTracking()
            .Where(a => a.AssignmentStatusLv.ValueCode != nameof(ShiftAssignmentStatusCode.CANCELLED)
                    && a.ShiftSchedule.BusinessDate >= fromDate
                    && a.ShiftSchedule.BusinessDate <= toDate
                    && a.AttendanceRecord != null
                    && (a.AttendanceRecord.AttendanceStatusLv.ValueCode == nameof(AttendanceStatusCode.LATE)
                        || a.AttendanceRecord.AttendanceStatusLv.ValueCode == nameof(AttendanceStatusCode.ABSENT)
                        || a.AttendanceRecord.AttendanceStatusLv.ValueCode == nameof(AttendanceStatusCode.EARLY_LEAVE)
                        || a.AttendanceRecord.IsManualAdjustment))
            .AsQueryable();

        if (staffId.HasValue)
            query = query.Where(a => a.StaffId == staffId.Value);

        return await query
            .OrderByDescending(a => a.ShiftSchedule.BusinessDate)
                .ThenBy(a => a.Staff.FullName)
            .ToListAsync(ct);
    }
}
