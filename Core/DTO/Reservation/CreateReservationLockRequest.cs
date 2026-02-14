using System.ComponentModel.DataAnnotations;

namespace Core.DTO.Reservation;

/// <summary>
/// Request DTO for creating a soft lock on a table (10 minutes).
/// </summary>
public class CreateReservationLockRequest
{
    /// <summary>
    /// List of table IDs to lock. If provided, TableId is ignored (or used as fallback/first item).
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

    [Required]
    [Range(1, 50)]
    public int PartySize { get; set; }

    [Required]
    public DateTime ReservedTime { get; set; }
}
