using System;
using System.Collections.Generic;

namespace Core.Entity;

public partial class RestaurantTable
{
    public long TableId { get; set; }

    public string TableCode { get; set; } = null!;

    public int Capacity { get; set; }

    /// <summary>
    /// TableStatus: 1=AVAILABLE,2=OCCUPIED,3=RESERVED,4=LOCKED
    /// </summary>
    public byte TableStatus { get; set; }

    public long? TableQrImg { get; set; }

    /// <summary>
    /// TableType (numeric enum in app)
    /// </summary>
    public byte TableType { get; set; }

    public uint TableStatusLvId { get; set; }

    public uint TableTypeLvId { get; set; }

    public bool? IsOnline { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<ServiceError> ServiceErrors { get; set; } = new List<ServiceError>();

    public virtual ICollection<TableMedium> TableMedia { get; set; } = new List<TableMedium>();

    public virtual MediaAsset? TableQrImgNavigation { get; set; }

    public virtual LookupValue TableStatusLv { get; set; } = null!;

    public virtual LookupValue TableTypeLv { get; set; } = null!;

    public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
