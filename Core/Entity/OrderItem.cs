using System;
using System.Collections.Generic;

namespace Core.Entity;

public partial class OrderItem
{
    public long OrderItemId { get; set; }

    public long OrderId { get; set; }

    public long DishId { get; set; }

    public int Quantity { get; set; }

    public decimal Price { get; set; }

    /// <summary>
    /// OrderItemStatus: 1=CREATED,2=IN_PROGRESS,3=READY,4=SERVED,5=REJECTED
    /// </summary>
    public byte ItemStatus { get; set; }

    public string? RejectReason { get; set; }

    public uint ItemStatusLvId { get; set; }

    public virtual Dish Dish { get; set; } = null!;

    public virtual LookupValue ItemStatusLv { get; set; } = null!;

    public virtual Order Order { get; set; } = null!;

    public virtual ICollection<ServiceError> ServiceErrors { get; set; } = new List<ServiceError>();
}
