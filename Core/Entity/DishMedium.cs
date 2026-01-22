using System;
using System.Collections.Generic;

namespace Core.Entity;

public partial class DishMedium
{
    public long DishId { get; set; }

    public long MediaId { get; set; }

    public bool? IsPrimary { get; set; }

    public virtual Dish Dish { get; set; } = null!;

    public virtual MediaAsset Media { get; set; } = null!;
}
