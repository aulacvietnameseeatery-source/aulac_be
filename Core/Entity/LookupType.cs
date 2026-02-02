using System;
using System.Collections.Generic;

namespace Core.Entity;

public partial class LookupType
{
    public ushort TypeId { get; set; }

    public string TypeCode { get; set; } = null!;

    public string TypeName { get; set; } = null!;

    public string? Description { get; set; }

    /// <summary>
    /// 1 = admin can add/remove values, 0 = controlled enum (statuses, workflows)
    /// </summary>
    public bool IsConfigurable { get; set; }

    /// <summary>
    /// 1 = system-defined enum type, 0 = user-defined/custom type
    /// </summary>
    public bool? IsSystem { get; set; }

    public long? TypeNameTextId { get; set; }

    public long? TypeDescTextId { get; set; }

    public virtual ICollection<LookupValue> LookupValues { get; set; } = new List<LookupValue>();

    public virtual I18nText? TypeDescText { get; set; }

    public virtual I18nText? TypeNameText { get; set; }
}
