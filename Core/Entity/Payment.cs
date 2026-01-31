using System;
using System.Collections.Generic;

namespace Core.Entity;

public partial class Payment
{
    public long PaymentId { get; set; }

    public long OrderId { get; set; }

    /// <summary>
    /// PaymentMethod (numeric enum in app)
    /// </summary>
    public byte MethodId { get; set; }

    public decimal ReceivedAmount { get; set; }

    public decimal ChangeAmount { get; set; }

    public DateTime? PaidAt { get; set; }

    public uint MethodLvId { get; set; }

    public virtual LookupValue MethodLv { get; set; } = null!;

    public virtual Order Order { get; set; } = null!;
}
