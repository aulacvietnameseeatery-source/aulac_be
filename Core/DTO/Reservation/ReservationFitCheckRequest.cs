using System;
using System.ComponentModel.DataAnnotations;

namespace Core.DTO.Reservation;

public class ReservationFitCheckRequest
{
    [Required]
    [Range(1, 50)]
    public int PartySize { get; set; }

    [Required]
    public DateTime ReservedTime { get; set; }
}
