using Core.DTO.Shift;
using Core.Entity;

namespace Core.Interface.Repo;

public interface IAttendanceRepository
{
    void Add(AttendanceRecord entity);

    Task<AttendanceRecord?> GetByAssignmentIdAsync(long shiftAssignmentId, CancellationToken ct = default);

    Task<AttendanceRecord?> GetByIdAsync(long attendanceId, CancellationToken ct = default);

    Task<AttendanceRecord?> GetByIdWithDetailsAsync(long attendanceId, CancellationToken ct = default);
}
