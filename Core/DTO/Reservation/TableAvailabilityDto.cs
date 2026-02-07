namespace Core.DTO.Reservation;

/// <summary>
/// DTO for displaying table availability to public users.
/// </summary>
public class TableAvailabilityDto
{
    public long TableId { get; set; }
    public string TableCode { get; set; } = null!;
    public int Capacity { get; set; }
    public string TableType { get; set; } = null!;
    public string Zone { get; set; } = null!;
    public bool IsAvailable { get; set; }
    public DateTime? LockedUntil { get; set; }
    public string? ImageUrl { get; set; }
}
