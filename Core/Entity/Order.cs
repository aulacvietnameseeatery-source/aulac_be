using System;
using System.Collections.Generic;

namespace Core.Entity;

public partial class Order
{
    public long OrderId { get; set; }

    public long TableId { get; set; }

    public long StaffId { get; set; }

    /// <summary>
    /// OrderSource (numeric enum in app)
    /// </summary>
    public byte SourceId { get; set; }

    /// <summary>
    /// OrderStatus: 1=PENDING,2=IN_PROGRESS,3=COMPLETED,4=CANCELLED
    /// </summary>
    public byte OrderStatus { get; set; }

    public DateTime? CreatedAt { get; set; }

    public long CustomerId { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal? TipAmount { get; set; }

    public uint SourceLvId { get; set; }

    public uint OrderStatusLvId { get; set; }

    public virtual Customer Customer { get; set; } = null!;

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<OrderPromotion> OrderPromotions { get; set; } = new List<OrderPromotion>();

    public virtual LookupValue OrderStatusLv { get; set; } = null!;

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<ServiceError> ServiceErrors { get; set; } = new List<ServiceError>();

    public virtual LookupValue SourceLv { get; set; } = null!;

    public virtual StaffAccount Staff { get; set; } = null!;

    public virtual RestaurantTable Table { get; set; } = null!;
}
