using Core.DTO.Shift;

namespace Core.Interface.Service.Shift;

public interface IShiftLiveRealtimePublisher
{
    Task PublishBoardChangedAsync(ShiftLiveRealtimeEventDto payload, CancellationToken ct = default);
}