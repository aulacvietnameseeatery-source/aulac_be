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
            .FirstOrDefaultAsync(ar => ar.AttendanceId == attendanceId, ct);
    }

    public async Task<AttendanceRecord?> GetByIdWithDetailsAsync(long attendanceId, CancellationToken ct = default)
    {
        return await _context.AttendanceRecords
            .Include(ar => ar.AttendanceStatusLv)
            .Include(ar => ar.ReviewedByStaff)
            .Include(ar => ar.ShiftAssignment)
                .ThenInclude(a => a.ShiftTemplate)
            .Include(ar => ar.ShiftAssignment)
                .ThenInclude(a => a.Staff)
            .FirstOrDefaultAsync(ar => ar.AttendanceId == attendanceId, ct);
    }
}
