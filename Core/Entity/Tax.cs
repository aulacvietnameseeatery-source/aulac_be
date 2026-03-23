using System;
using System.Collections.Generic;

namespace Core.Entity;

public partial class Tax
{
    public long TaxId { get; set; }

    public string TaxName { get; set; } = null!;

    public decimal TaxRate { get; set; }

    public string TaxType { get; set; } = "EXCLUSIVE";

    public bool IsActive { get; set; } = true;

    public bool IsDefault { get; set; } = false;

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
