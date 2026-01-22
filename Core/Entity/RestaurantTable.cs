using System;
using System.Collections.Generic;

namespace Core.Entity;

public partial class RestaurantTable
{
    public long TableId { get; set; }

    public string TableCode { get; set; } = null!;

    public int Capacity { get; set; }

    public long StatusId { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<ServiceError> ServiceErrors { get; set; } = new List<ServiceError>();

    public virtual SettingItem Status { get; set; } = null!;

    public virtual ICollection<TableMedium> TableMedia { get; set; } = new List<TableMedium>();

    public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
