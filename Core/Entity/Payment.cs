using System;
using System.Collections.Generic;
using Core.Enum;

namespace Core.Entity;

public partial class Payment
{
    public long PaymentId { get; set; }

    public long OrderId { get; set; }

    public PaymentMethod MethodId { get; set; }

    public decimal ReceivedAmount { get; set; }

    public decimal ChangeAmount { get; set; }

    public DateTime? PaidAt { get; set; }

    public virtual Order Order { get; set; } = null!;
}
