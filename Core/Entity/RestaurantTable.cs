using System;
using System.Collections.Generic;
using Core.Enum;

namespace Core.Entity;

public partial class RestaurantTable
{
    public long TableId { get; set; }

    public string TableCode { get; set; } = null!;

    public int Capacity { get; set; }

    public TableStatus TableStatus { get; set; }

    public long? TableQrImg { get; set; }

    public TableType TableType { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<ServiceError> ServiceErrors { get; set; } = new List<ServiceError>();

    public virtual ICollection<TableMedium> TableMedia { get; set; } = new List<TableMedium>();

    public virtual MediaAsset? TableQrImgNavigation { get; set; }

    public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
