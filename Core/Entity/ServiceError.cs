using System;
using System.Collections.Generic;

namespace Core.Entity;

public partial class ServiceError
{
    public long ErrorId { get; set; }

    public long StaffId { get; set; }

    public long? OrderId { get; set; }

    public long? OrderItemId { get; set; }

    public long? TableId { get; set; }

    public long CategoryId { get; set; }

    public long SeverityId { get; set; }

    public string Description { get; set; } = null!;

    public decimal? PenaltyAmount { get; set; }

    public bool? IsResolved { get; set; }

    public long? ResolvedBy { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ServiceErrorCategory Category { get; set; } = null!;

    public virtual Order? Order { get; set; }

    public virtual OrderItem? OrderItem { get; set; }

    public virtual StaffAccount? ResolvedByNavigation { get; set; }

    public virtual SettingItem Severity { get; set; } = null!;

    public virtual StaffAccount Staff { get; set; } = null!;

    public virtual RestaurantTable? Table { get; set; }
}
