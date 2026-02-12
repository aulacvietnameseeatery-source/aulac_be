namespace Core.Interface.Service.Others;

public interface IReservationBroadcastService
{
    Task BroadcastTableLockedAsync(long tableId, DateTime lockedUntil);
    Task BroadcastTableUnlockedAsync(long tableId);
    Task BroadcastReservationCreatedAsync(long reservationId, long tableId);
    Task BroadcastReservationStatusChangedAsync(long reservationId, string status);
}
