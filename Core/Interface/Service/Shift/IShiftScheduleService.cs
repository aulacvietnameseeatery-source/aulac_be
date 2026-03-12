using Core.DTO.Shift;

namespace Core.Interface.Service.Shift;

public interface IShiftScheduleService
{
    Task<(List<ShiftScheduleListDto> Items, int TotalCount)> GetSchedulesAsync(
        GetShiftScheduleRequest request, CancellationToken ct = default);

    Task<ShiftScheduleDetailDto> GetScheduleByIdAsync(long id, CancellationToken ct = default);

    Task<ShiftScheduleDetailDto> CreateScheduleAsync(
        CreateShiftScheduleRequest request, long createdByStaffId, CancellationToken ct = default);

    Task<ShiftScheduleDetailDto> UpdateScheduleAsync(
        long id, UpdateShiftScheduleRequest request, long updatedByStaffId, CancellationToken ct = default);
}
