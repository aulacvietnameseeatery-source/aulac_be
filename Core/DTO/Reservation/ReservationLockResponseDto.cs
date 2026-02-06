namespace Core.DTO.Reservation;

/// <summary>
/// Response DTO for soft lock operation.
/// </summary>
public class ReservationLockResponseDto
{
    public string LockToken { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public long TableId { get; set; }
    public string TableCode { get; set; } = null!;
}
