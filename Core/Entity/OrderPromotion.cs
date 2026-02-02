using System;
using System.Collections.Generic;

namespace Core.Entity;

public partial class OrderPromotion
{
    public long OrderPromotionId { get; set; }

    public long OrderId { get; set; }

    public long PromotionId { get; set; }

    public decimal DiscountAmount { get; set; }

    public DateTime? AppliedAt { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual Promotion Promotion { get; set; } = null!;
}
