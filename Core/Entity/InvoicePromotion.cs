using System;
using System.Collections.Generic;

namespace Core.Entity;

public partial class InvoicePromotion
{
    public long InvoicePromotionId { get; set; }

    public long InvoiceId { get; set; }

    public long PromotionId { get; set; }

    public decimal DiscountAmount { get; set; }

    public DateTime? AppliedAt { get; set; }

    public virtual Invoice Invoice { get; set; } = null!;

    public virtual Promotion Promotion { get; set; } = null!;
}
