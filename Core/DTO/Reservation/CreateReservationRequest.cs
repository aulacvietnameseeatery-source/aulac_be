using System.ComponentModel.DataAnnotations;

namespace Core.DTO.Reservation;

/// <summary>
/// Request DTO for submitting a final reservation.
/// </summary>
public class CreateReservationRequest
{
    public string? LockToken { get; set; }
    
    /// <summary>
    /// List of table IDs to reserve. If provided, TableId is ignored (or used as fallback/first item).
    /// </summary>
    public List<long>? TableIds { get; set; }

    [Required]
    public long TableId { get; set; }

    [Required]
    [StringLength(150, MinimumLength = 2)]
    public string CustomerName { get; set; } = null!;

    [Required]
    [Phone]
    [StringLength(30)]
    public string Phone { get; set; } = null!;

    [EmailAddress]
    [StringLength(150)]
    public string? Email { get; set; }

    [Required]
    [Range(1, 50)]
    public int PartySize { get; set; }

    [Required]
    public DateTime ReservedTime { get; set; }
}
