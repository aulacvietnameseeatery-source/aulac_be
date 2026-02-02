using System;
using System.Collections.Generic;

namespace Core.Entity;

public partial class AuditLog
{
    public long LogId { get; set; }

    public long? StaffId { get; set; }

    public string? ActionCode { get; set; }

    public string? TargetTable { get; set; }

    public long? TargetId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual StaffAccount? Staff { get; set; }
}
