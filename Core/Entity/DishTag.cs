using System;
using System.Collections.Generic;

namespace Core.Entity;

public partial class DishTag
{
    public long DishTagId { get; set; }

    public long DishId { get; set; }

    public uint TagId { get; set; }

    public virtual Dish Dish { get; set; } = null!;

    public virtual LookupValue Tag { get; set; } = null!;
}
