using System;
using System.Collections.Generic;

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

    /// <summary>
    /// ReservationSource (numeric enum in app)
    /// </summary>
    public byte SourceId { get; set; }

    /// <summary>
    /// ReservationStatus: 1=PENDING,2=CONFIRMED,3=CHECKED_IN,4=CANCELLED,5=NO_SHOW
    /// </summary>
    public byte ReservationStatus { get; set; }

    public DateTime? CreatedAt { get; set; }

    public uint SourceLvId { get; set; }

    public uint ReservationStatusLvId { get; set; }

    public virtual Customer? Customer { get; set; }

    public virtual LookupValue ReservationStatusLv { get; set; } = null!;

    public virtual LookupValue SourceLv { get; set; } = null!;

    public virtual ICollection<RestaurantTable> Tables { get; set; } = new List<RestaurantTable>();
}
