using Core.DTO.Shift;
using Core.Entity;
using Core.Interface.Repo;
using Infa.Data;
using Microsoft.EntityFrameworkCore;

namespace Infa.Repo;

public class ShiftAssignmentRepository : IShiftAssignmentRepository
{
    private readonly RestaurantMgmtContext _context;

    public ShiftAssignmentRepository(RestaurantMgmtContext context)
    {
        _context = context;
    }

    public void Add(ShiftAssignment entity) => _context.ShiftAssignments.Add(entity);

    public async Task<ShiftAssignment?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        return await _context.ShiftAssignments
            .Include(a => a.ShiftTemplate)
            .Include(a => a.AttendanceRecord)
            .FirstOrDefaultAsync(a => a.ShiftAssignmentId == id, ct);
    }

    public async Task<ShiftAssignment?> GetByIdWithDetailsAsync(long id, CancellationToken ct = default)
    {
        return await _context.ShiftAssignments
            .Include(a => a.ShiftTemplate)
            .Include(a => a.Staff)
            .Include(a => a.AssignedByStaff)
            .Include(a => a.AttendanceRecord)
                .ThenInclude(ar => ar!.AttendanceStatusLv)
            .Include(a => a.AttendanceRecord)
                .ThenInclude(ar => ar!.ReviewedByStaff)
            .FirstOrDefaultAsync(a => a.ShiftAssignmentId == id, ct);
    }

    public async Task<(List<ShiftAssignment> Items, int TotalCount)> GetAssignmentsAsync(
        GetShiftAssignmentRequest request, CancellationToken ct = default)
    {
        var query = _context.ShiftAssignments
            .Include(a => a.ShiftTemplate)
            .Include(a => a.Staff)
            .Include(a => a.AssignedByStaff)
            .Include(a => a.AttendanceRecord)
                .ThenInclude(ar => ar!.AttendanceStatusLv)
            .AsNoTracking()
            .AsQueryable();

        if (request.StaffId.HasValue)
            query = query.Where(a => a.StaffId == request.StaffId.Value);

        if (request.ShiftTemplateId.HasValue)
            query = query.Where(a => a.ShiftTemplateId == request.ShiftTemplateId.Value);

        if (request.FromDate.HasValue)
            query = query.Where(a => a.WorkDate >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            query = query.Where(a => a.WorkDate <= request.ToDate.Value);

        if (request.IsActive.HasValue)
            query = query.Where(a => a.IsActive == request.IsActive.Value);

        var totalCount = await query.CountAsync(ct);

        var pageIndex = request.PageIndex < 1 ? 1 : request.PageIndex;
        var pageSize = request.PageSize < 1 ? 20 : request.PageSize;

        var items = await query
            .OrderByDescending(a => a.WorkDate)
                .ThenBy(a => a.PlannedStartAt)
                .ThenBy(a => a.Staff.FullName)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<bool> HasOverlappingAssignmentAsync(
        long staffId, DateOnly workDate, DateTime plannedStart, DateTime plannedEnd,
        long? excludeAssignmentId = null, CancellationToken ct = default)
    {
        var query = _context.ShiftAssignments
            .AsNoTracking()
            .Where(a => a.StaffId == staffId
                        && a.WorkDate == workDate
                        && a.IsActive
                        && a.PlannedStartAt < plannedEnd
                        && a.PlannedEndAt > plannedStart);

        if (excludeAssignmentId.HasValue)
            query = query.Where(a => a.ShiftAssignmentId != excludeAssignmentId.Value);

        return await query.AnyAsync(ct);
    }

    public async Task<List<long>> GetAlreadyAssignedStaffIdsAsync(
        long shiftTemplateId, DateOnly workDate, IEnumerable<long> staffIds,
        CancellationToken ct = default)
    {
        var ids = staffIds.ToList();
        return await _context.ShiftAssignments
            .AsNoTracking()
            .Where(a => a.ShiftTemplateId == shiftTemplateId
                        && a.WorkDate == workDate
                        && a.IsActive
                        && ids.Contains(a.StaffId))
            .Select(a => a.StaffId)
            .ToListAsync(ct);
    }

    public async Task<List<ShiftAssignment>> GetWorkedHoursDataAsync(
        DateOnly fromDate, DateOnly toDate, long? staffId, CancellationToken ct = default)
    {
        var query = _context.ShiftAssignments
            .Include(a => a.Staff)
            .Include(a => a.AttendanceRecord)
            .AsNoTracking()
            .Where(a => a.IsActive && a.WorkDate >= fromDate && a.WorkDate <= toDate)
            .AsQueryable();

        if (staffId.HasValue)
            query = query.Where(a => a.StaffId == staffId.Value);

        return await query.ToListAsync(ct);
    }

    public async Task<List<ShiftAssignment>> GetExceptionDataAsync(
        DateOnly fromDate, DateOnly toDate, long? staffId, CancellationToken ct = default)
    {
        var query = _context.ShiftAssignments
            .Include(a => a.ShiftTemplate)
            .Include(a => a.Staff)
            .Include(a => a.AttendanceRecord)
                .ThenInclude(ar => ar!.AttendanceStatusLv)
            .Include(a => a.AttendanceRecord)
                .ThenInclude(ar => ar!.ReviewedByStaff)
            .AsNoTracking()
            .Where(a => a.IsActive
                        && a.WorkDate >= fromDate
                        && a.WorkDate <= toDate
                        && a.AttendanceRecord != null
                        && (a.AttendanceRecord.AttendanceStatusLv.ValueCode == nameof(AttendanceStatusCode.LATE)
                            || a.AttendanceRecord.AttendanceStatusLv.ValueCode == nameof(AttendanceStatusCode.ABSENT)
                            || a.AttendanceRecord.AttendanceStatusLv.ValueCode == nameof(AttendanceStatusCode.EARLY_LEAVE)
                            || a.AttendanceRecord.IsManualAdjustment))
            .AsQueryable();

        if (staffId.HasValue)
            query = query.Where(a => a.StaffId == staffId.Value);

        return await query
            .OrderByDescending(a => a.WorkDate)
                .ThenBy(a => a.Staff.FullName)
            .ToListAsync(ct);
    }

    public async Task<(List<ShiftAssignment> Items, int TotalCount)> GetAttendanceReportAsync(
        AttendanceReportRequest request, CancellationToken ct = default)
    {
        var query = _context.ShiftAssignments
            .Include(a => a.ShiftTemplate)
            .Include(a => a.Staff)
            .Include(a => a.AttendanceRecord)
                .ThenInclude(ar => ar!.AttendanceStatusLv)
            .Include(a => a.AttendanceRecord)
                .ThenInclude(ar => ar!.ReviewedByStaff)
            .AsNoTracking()
            .Where(a => a.IsActive)
            .AsQueryable();

        if (request.FromDate.HasValue)
            query = query.Where(a => a.WorkDate >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            query = query.Where(a => a.WorkDate <= request.ToDate.Value);

        if (request.StaffId.HasValue)
            query = query.Where(a => a.StaffId == request.StaffId.Value);

        if (request.ShiftTemplateId.HasValue)
            query = query.Where(a => a.ShiftTemplateId == request.ShiftTemplateId.Value);

        if (!string.IsNullOrEmpty(request.AttendanceStatusCode))
            query = query.Where(a => a.AttendanceRecord != null
                && a.AttendanceRecord.AttendanceStatusLv.ValueCode == request.AttendanceStatusCode);

        var totalCount = await query.CountAsync(ct);

        var pageIndex = request.PageIndex < 1 ? 1 : request.PageIndex;
        var pageSize = request.PageSize < 1 ? 20 : request.PageSize;

        var items = await query
            .OrderByDescending(a => a.WorkDate)
                .ThenBy(a => a.Staff.FullName)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }
}
