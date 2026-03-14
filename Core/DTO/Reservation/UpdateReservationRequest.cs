using System;

namespace Core.DTO.Reservation
{
    public class UpdateReservationRequest
    {
        public string CustomerName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Email { get; set; }
        public int PartySize { get; set; }
        public DateTime ReservedTime { get; set; }
        public string? Notes { get; set; }
        public uint? StatusId { get; set; } // Optional: allow updating status in the edit modal if needed
        public System.Collections.Generic.List<long>? TableIds { get; set; }
    }
}
