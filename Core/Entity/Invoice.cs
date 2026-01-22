using System;
using System.Collections.Generic;

namespace Core.Entity;

public partial class Invoice
{
    public long InvoiceId { get; set; }

    public long OrderId { get; set; }

    public long StaffId { get; set; }

    public long? CustomerId { get; set; }

    public long StatusId { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal? TipAmount { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Customer? Customer { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual Account Staff { get; set; } = null!;

    public virtual SettingItem Status { get; set; } = null!;
}
