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

    public long StatusId { get; set; }

    public string? RejectReason { get; set; }

    public virtual Dish Dish { get; set; } = null!;

    public virtual Order Order { get; set; } = null!;

    public virtual ICollection<ServiceError> ServiceErrors { get; set; } = new List<ServiceError>();

    public virtual SettingItem Status { get; set; } = null!;
}
