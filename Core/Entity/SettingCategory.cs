using System;
using System.Collections.Generic;

namespace Core.Entity;

public partial class SettingCategory
{
    public long CategoryId { get; set; }

    public string CategoryCode { get; set; } = null!;

    public string CategoryName { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<SettingItem> SettingItems { get; set; } = new List<SettingItem>();
}
