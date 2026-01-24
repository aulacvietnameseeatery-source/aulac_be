using System;
using System.Collections.Generic;

namespace Core.Entity;

public partial class SettingItem
{
    public long SettingId { get; set; }

    public long CategoryId { get; set; }

    public string SettingCode { get; set; } = null!;

    public string SettingName { get; set; } = null!;

    public int? SortOrder { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<Account> Accounts { get; set; } = new List<Account>();

    public virtual SettingCategory Category { get; set; } = null!;

    public virtual ICollection<Dish> Dishes { get; set; } = new List<Dish>();

    public virtual ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    public virtual ICollection<MediaAsset> MediaAssets { get; set; } = new List<MediaAsset>();

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<Order> OrderSources { get; set; } = new List<Order>();

    public virtual ICollection<Order> OrderStatuses { get; set; } = new List<Order>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<Promotion> Promotions { get; set; } = new List<Promotion>();

    public virtual ICollection<Reservation> ReservationSources { get; set; } = new List<Reservation>();

    public virtual ICollection<Reservation> ReservationStatuses { get; set; } = new List<Reservation>();

    public virtual ICollection<RestaurantTable> RestaurantTableStatuses { get; set; } = new List<RestaurantTable>();

    public virtual ICollection<RestaurantTable> RestaurantTableTableTypeNavigations { get; set; } = new List<RestaurantTable>();

    public virtual ICollection<ServiceError> ServiceErrors { get; set; } = new List<ServiceError>();
}
