using System;
using System.Collections.Generic;

namespace Core.Entity;

public partial class TableMedium
{
    public long TableId { get; set; }

    public long MediaId { get; set; }

    public bool? IsPrimary { get; set; }

    public virtual MediaAsset Media { get; set; } = null!;

    public virtual RestaurantTable Table { get; set; } = null!;
}
