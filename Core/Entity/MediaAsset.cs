using System;
using System.Collections.Generic;

namespace Core.Entity;

public partial class MediaAsset
{
    public long MediaId { get; set; }

    public string Url { get; set; } = null!;

    public string? MimeType { get; set; }

    public int? Width { get; set; }

    public int? Height { get; set; }

    public int? DurationSec { get; set; }

    public DateTime? CreatedAt { get; set; }

    public uint MediaTypeLvId { get; set; }

    public virtual ICollection<DishMedium> DishMedia { get; set; } = new List<DishMedium>();

    public virtual ICollection<Ingredient> Ingredients { get; set; } = new List<Ingredient>();

    public virtual ICollection<InventoryTransactionMedium> InventoryTransactionMedia { get; set; } = new List<InventoryTransactionMedium>();

    public virtual LookupValue MediaTypeLv { get; set; } = null!;

    public virtual ICollection<RestaurantTable> RestaurantTables { get; set; } = new List<RestaurantTable>();

    public virtual ICollection<TableMedium> TableMedia { get; set; } = new List<TableMedium>();
}
