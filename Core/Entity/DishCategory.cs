using System;
using System.Collections.Generic;

namespace Core.Entity;

public partial class DishCategory
{
    public long CategoryId { get; set; }

    public string CategoryName { get; set; } = null!;

    public virtual ICollection<Dish> Dishes { get; set; } = new List<Dish>();
}
