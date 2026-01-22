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

    public long SourceId { get; set; }

    public long StatusId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Customer? Customer { get; set; }

    public virtual SettingItem Source { get; set; } = null!;

    public virtual SettingItem Status { get; set; } = null!;

    public virtual ICollection<RestaurantTable> Tables { get; set; } = new List<RestaurantTable>();
}
