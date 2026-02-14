using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO.Reservation
{
    public class CreateManualReservationRequest
    {
        public string? LockToken { get; set; }

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

        [Required]
        public string Status { get; set; } = null!;
        [Required]
        public string Source { get; set; } = null!;
    }
}
