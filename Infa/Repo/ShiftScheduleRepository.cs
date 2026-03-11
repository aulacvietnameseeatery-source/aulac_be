using Core.DTO.Shift;
using Core.Entity;
using Core.Interface.Repo;
using Infa.Data;
using Microsoft.EntityFrameworkCore;

namespace Infa.Repo;

public class ShiftScheduleRepository : IShiftScheduleRepository
{
    private readonly RestaurantMgmtContext _context;

    public ShiftScheduleRepository(RestaurantMgmtContext context)
    {
        _context = context;
    }

    public void Add(ShiftSchedule entity) => _context.ShiftSchedules.Add(entity);

    public async Task<ShiftSchedule?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        return await _context.ShiftSchedules
            .Include(s => s.ShiftTypeLv)
            .Include(s => s.StatusLv)
            .FirstOrDefaultAsync(s => s.ShiftScheduleId == id, ct);
    }

    public async Task<ShiftSchedule?> GetByIdWithDetailsAsync(long id, CancellationToken ct = default)
    {
        return await _context.ShiftSchedules
            .Include(s => s.ShiftTypeLv)
            .Include(s => s.StatusLv)
            .Include(s => s.CreatedByStaff)
            .Include(s => s.UpdatedByStaff)
            .Include(s => s.ShiftAssignments)
                .ThenInclude(a => a.Staff)
            .Include(s => s.ShiftAssignments)
                .ThenInclude(a => a.Role)
            .Include(s => s.ShiftAssignments)
                .ThenInclude(a => a.AssignmentStatusLv)
            .Include(s => s.ShiftAssignments)
                .ThenInclude(a => a.AssignedByStaff)
            .Include(s => s.ShiftAssignments)
                .ThenInclude(a => a.AttendanceRecord)
                    .ThenInclude(ar => ar!.AttendanceStatusLv)
            .Include(s => s.ShiftAssignments)
                .ThenInclude(a => a.AttendanceRecord)
                    .ThenInclude(ar => ar!.ReviewedByStaff)
            .FirstOrDefaultAsync(s => s.ShiftScheduleId == id, ct);
    }

    public async Task<(List<ShiftSchedule> Items, int TotalCount)> GetSchedulesAsync(
        GetShiftScheduleRequest request, CancellationToken ct = default)
    {
        var query = _context.ShiftSchedules
            .Include(s => s.ShiftTypeLv)
            .Include(s => s.StatusLv)
            .Include(s => s.ShiftAssignments)
            .AsNoTracking()
            .AsQueryable();

        if (request.FromDate.HasValue)
            query = query.Where(s => s.BusinessDate >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            query = query.Where(s => s.BusinessDate <= request.ToDate.Value);

        if (request.ShiftTypeLvId.HasValue)
            query = query.Where(s => s.ShiftTypeLvId == request.ShiftTypeLvId.Value);

        if (request.StatusLvId.HasValue)
            query = query.Where(s => s.StatusLvId == request.StatusLvId.Value);

        var totalCount = await query.CountAsync(ct);

        var pageIndex = request.PageIndex < 1 ? 1 : request.PageIndex;
        var pageSize = request.PageSize < 1 ? 20 : request.PageSize;

        var items = await query
            .OrderByDescending(s => s.BusinessDate)
                .ThenBy(s => s.PlannedStartAt)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<bool> HasOverlappingScheduleAsync(
     DateOnly businessDate, uint shiftTypeLvId, DateTime plannedStart, DateTime plannedEnd,
        long? excludeId = null, CancellationToken ct = default)
    {
        var query = _context.ShiftSchedules
            .AsNoTracking()
            .Where(s => s.BusinessDate == businessDate
                    && s.ShiftTypeLvId == shiftTypeLvId
                    && s.StatusLv.ValueCode != nameof(ShiftStatusCode.CANCELLED)
                    && s.PlannedStartAt < plannedEnd
                    && s.PlannedEndAt > plannedStart);

        if (excludeId.HasValue)
            query = query.Where(s => s.ShiftScheduleId != excludeId.Value);

        return await query.AnyAsync(ct);
    }

    public async Task<bool> IsValidLookupAsync(uint lvId, ushort typeId, CancellationToken ct = default)
    {
        return await _context.LookupValues
            .AsNoTracking()
            .AnyAsync(lv => lv.ValueId == lvId && lv.TypeId == typeId && lv.IsActive == true && lv.DeletedAt == null, ct);
    }
}
