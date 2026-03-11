using Core.DTO.Shift;
using Core.Entity;

namespace Core.Interface.Repo;

public interface IShiftScheduleRepository
{
    void Add(ShiftSchedule entity);

    Task<ShiftSchedule?> GetByIdAsync(long id, CancellationToken ct = default);

    Task<ShiftSchedule?> GetByIdWithDetailsAsync(long id, CancellationToken ct = default);

    Task<(List<ShiftSchedule> Items, int TotalCount)> GetSchedulesAsync(
        GetShiftScheduleRequest request, CancellationToken ct = default);

    /// <summary>
    /// Returns true if an overlapping published/draft shift of the same type exists
    /// on the same business date, excluding <paramref name="excludeId"/>.
    /// </summary>
    Task<bool> HasOverlappingScheduleAsync(
        DateOnly businessDate, uint shiftTypeLvId, DateTime plannedStart, DateTime plannedEnd,
        long? excludeId = null, CancellationToken ct = default);

    /// <summary>Validates a lookup value belongs to the given type and is active.</summary>
    Task<bool> IsValidLookupAsync(uint lvId, ushort typeId, CancellationToken ct = default);
}
