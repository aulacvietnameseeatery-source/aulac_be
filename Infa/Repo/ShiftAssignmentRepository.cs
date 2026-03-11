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

    public void AddRange(IEnumerable<ShiftAssignment> entities) => _context.ShiftAssignments.AddRange(entities);

    public async Task<ShiftAssignment?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        return await _context.ShiftAssignments
            .Include(a => a.AssignmentStatusLv)
            .Include(a => a.ShiftSchedule)
                .ThenInclude(s => s.ShiftTypeLv)
            .Include(a => a.ShiftSchedule)
                .ThenInclude(s => s.StatusLv)
            .Include(a => a.Staff)
            .Include(a => a.AttendanceRecord)
                .ThenInclude(ar => ar!.AttendanceStatusLv)
            .FirstOrDefaultAsync(a => a.ShiftAssignmentId == id, ct);
    }

    public async Task<ShiftAssignment?> GetByIdWithDetailsAsync(long id, CancellationToken ct = default)
    {
        return await _context.ShiftAssignments
              .Include(a => a.AssignmentStatusLv)
              .Include(a => a.ShiftSchedule)
                  .ThenInclude(s => s.ShiftTypeLv)
              .Include(a => a.ShiftSchedule)
                  .ThenInclude(s => s.StatusLv)
              .Include(a => a.Staff)
              .Include(a => a.Role)
              .Include(a => a.AssignedByStaff)
              .Include(a => a.AttendanceRecord)
                  .ThenInclude(ar => ar!.AttendanceStatusLv)
              .Include(a => a.AttendanceRecord)
                  .ThenInclude(ar => ar!.ReviewedByStaff)
              .FirstOrDefaultAsync(a => a.ShiftAssignmentId == id, ct);
    }

    public async Task<(List<ShiftAssignment> Items, int TotalCount)> GetAssignmentsAsync(GetShiftAssignmentRequest request, CancellationToken ct = default)
    {
        var query = _context.ShiftAssignments
                 .Include(a => a.AssignmentStatusLv)
                 .Include(a => a.ShiftSchedule)
                     .ThenInclude(s => s.ShiftTypeLv)
                 .Include(a => a.ShiftSchedule)
                     .ThenInclude(s => s.StatusLv)
                 .Include(a => a.Staff)
                 .Include(a => a.Role)
                 .Include(a => a.AssignedByStaff)
                 .Include(a => a.AttendanceRecord)
                     .ThenInclude(ar => ar!.AttendanceStatusLv)
                 .AsNoTracking()
                 .AsQueryable();

        if (request.ShiftScheduleId.HasValue)
        { query = query.Where(a => a.ShiftScheduleId == request.ShiftScheduleId.Value); }

        if (request.StaffId.HasValue)
            query = query.Where(a => a.StaffId == request.StaffId.Value);

        if (request.AssignmentStatusLvId.HasValue)
            query = query.Where(a => a.AssignmentStatusLvId == request.AssignmentStatusLvId.Value);

        if (request.FromDate.HasValue)
            query = query.Where(a => a.ShiftSchedule.BusinessDate >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            query = query.Where(a => a.ShiftSchedule.BusinessDate <= request.ToDate.Value);

        var totalCount = await query.CountAsync(ct);

        var pageIndex = request.PageIndex < 1 ? 1 : request.PageIndex;
        var pageSize = request.PageSize < 1 ? 20 : request.PageSize;

        var items = await query
            .OrderByDescending(a => a.ShiftSchedule.BusinessDate)
                .ThenBy(a => a.ShiftSchedule.PlannedStartAt)
                .ThenBy(a => a.Staff.FullName)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<bool> HasOverlappingAssignmentAsync(
        long staffId, DateTime plannedStart, DateTime plannedEnd,
        long? excludeAssignmentId = null, CancellationToken ct = default)
    {
        var query = _context.ShiftAssignments
              .AsNoTracking()
              .Where(a => a.StaffId == staffId
                    && a.AssignmentStatusLv.ValueCode != nameof(ShiftAssignmentStatusCode.CANCELLED)
                    && a.ShiftSchedule.PlannedStartAt < plannedEnd
                    && a.ShiftSchedule.PlannedEndAt > plannedStart);

        if (excludeAssignmentId.HasValue)
            query = query.Where(a => a.ShiftAssignmentId != excludeAssignmentId.Value);

        return await query.AnyAsync(ct);
    }

    public async Task<List<long>> GetAlreadyAssignedStaffIdsAsync(
   long shiftScheduleId, IEnumerable<long> staffIds, CancellationToken ct = default)
    {
        var ids = staffIds.ToList();
        return await _context.ShiftAssignments
            .AsNoTracking()
            .Where(a => a.ShiftScheduleId == shiftScheduleId
                    && ids.Contains(a.StaffId)
                    && a.AssignmentStatusLv.ValueCode != nameof(ShiftAssignmentStatusCode.CANCELLED))
            .Select(a => a.StaffId)
            .ToListAsync(ct);
    }
}
