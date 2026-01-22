using System;
using System.Collections.Generic;

namespace Core.Entity;

public partial class Order
{
    public long OrderId { get; set; }

    public long TableId { get; set; }

    public long StaffId { get; set; }

    public long SourceId { get; set; }

    public long StatusId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Invoice? Invoice { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<ServiceError> ServiceErrors { get; set; } = new List<ServiceError>();

    public virtual SettingItem Source { get; set; } = null!;

    public virtual Account Staff { get; set; } = null!;

    public virtual SettingItem Status { get; set; } = null!;

    public virtual RestaurantTable Table { get; set; } = null!;
}
