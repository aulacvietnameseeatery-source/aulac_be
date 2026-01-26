using System;
using System.Collections.Generic;

namespace Core.Entity;

public partial class Customer
{
    public long CustomerId { get; set; }

    public string? FullName { get; set; }

    public string Phone { get; set; } = null!;

    public string? Email { get; set; }

    public bool? IsMember { get; set; }

    public int? LoyaltyPoints { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
