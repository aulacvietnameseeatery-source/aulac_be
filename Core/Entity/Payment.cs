using System;
using System.Collections.Generic;

namespace Core.Entity;

public partial class Payment
{
    public long PaymentId { get; set; }

    public long OrderId { get; set; }

    public long MethodId { get; set; }

    public decimal ReceivedAmount { get; set; }

    public decimal ChangeAmount { get; set; }

    public DateTime? PaidAt { get; set; }

    public virtual SettingItem Method { get; set; } = null!;

    public virtual Order Order { get; set; } = null!;
}
