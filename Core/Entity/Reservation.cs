using System;
using System.Collections.Generic;
using Core.Enum;

namespace Core.Entity;

public partial class Reservation
{
    public long ReservationId { get; set; }

    public long? CustomerId { get; set; }

    public string CustomerName { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public string? Email { get; set; }

    public int PartySize { get; set; }

    public DateTime ReservedTime { get; set; }

    public ReservationSource SourceId { get; set; }

    public ReservationStatus ReservationStatus { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Customer? Customer { get; set; }

    public virtual ICollection<RestaurantTable> Tables { get; set; } = new List<RestaurantTable>();
}
