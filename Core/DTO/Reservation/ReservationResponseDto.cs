namespace Core.DTO.Reservation;

/// <summary>
/// Response DTO for confirmed reservation.
/// </summary>
public class ReservationResponseDto
{
    public long ReservationId { get; set; }
    public string CustomerName { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string? Email { get; set; }
    public int PartySize { get; set; }
    public DateTime ReservedTime { get; set; }
    public string TableCode { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
