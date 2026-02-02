using System;
using System.Collections.Generic;

namespace Core.Entity;

public partial class SystemSetting
{
    public uint SettingId { get; set; }

    public string SettingKey { get; set; } = null!;

    public string ValueType { get; set; } = null!;

    public string? ValueString { get; set; }

    public long? ValueInt { get; set; }

    public decimal? ValueDecimal { get; set; }

    public bool? ValueBool { get; set; }

    public string? ValueJson { get; set; }

    public string? Description { get; set; }

    public bool IsSensitive { get; set; }

    public DateTime UpdatedAt { get; set; }

    public long? UpdatedBy { get; set; }

    public virtual StaffAccount? UpdatedByNavigation { get; set; }
}
