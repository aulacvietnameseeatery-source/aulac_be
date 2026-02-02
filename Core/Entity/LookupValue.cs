using System;
using System.Collections.Generic;

namespace Core.Entity;

public partial class LookupValue
{
    public uint ValueId { get; set; }

    public ushort TypeId { get; set; }

    public string ValueCode { get; set; } = null!;

    public string ValueName { get; set; } = null!;

    public short SortOrder { get; set; }

    public bool? IsActive { get; set; }

    public string? Meta { get; set; }

    /// <summary>
    /// 1 = system/seeded value, 0 = user-added value
    /// </summary>
    public bool? IsSystem { get; set; }

    /// <summary>
    /// 1 = value_code cannot be changed and value cannot be deleted
    /// </summary>
    public bool? Locked { get; set; }

    /// <summary>
    /// Soft delete timestamp; never hard delete lookup values
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    public string? Description { get; set; }

    public DateTime? UpdateAt { get; set; }

    public virtual ICollection<Dish> Dishes { get; set; } = new List<Dish>();

    public virtual ICollection<Ingredient> Ingredients { get; set; } = new List<Ingredient>();

    public virtual ICollection<InventoryTransaction> InventoryTransactionStatusLvs { get; set; } = new List<InventoryTransaction>();

    public virtual ICollection<InventoryTransaction> InventoryTransactionTypeLvs { get; set; } = new List<InventoryTransaction>();

    public virtual ICollection<MediaAsset> MediaAssets { get; set; } = new List<MediaAsset>();

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<Order> OrderOrderStatusLvs { get; set; } = new List<Order>();

    public virtual ICollection<Order> OrderSourceLvs { get; set; } = new List<Order>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<Promotion> PromotionPromotionStatusLvs { get; set; } = new List<Promotion>();

    public virtual ICollection<Promotion> PromotionTypeLvs { get; set; } = new List<Promotion>();

    public virtual ICollection<Reservation> ReservationReservationStatusLvs { get; set; } = new List<Reservation>();

    public virtual ICollection<Reservation> ReservationSourceLvs { get; set; } = new List<Reservation>();

    public virtual ICollection<RestaurantTable> RestaurantTableTableStatusLvs { get; set; } = new List<RestaurantTable>();

    public virtual ICollection<RestaurantTable> RestaurantTableTableTypeLvs { get; set; } = new List<RestaurantTable>();

    public virtual ICollection<ServiceError> ServiceErrors { get; set; } = new List<ServiceError>();

    public virtual LookupType Type { get; set; } = null!;
}
