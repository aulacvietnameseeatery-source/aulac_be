namespace Core.DTO.Reservation;

public class ReservationFitCheckResponse
{
    public bool CanBookOnline { get; set; }
    public string? Message { get; set; }
}
