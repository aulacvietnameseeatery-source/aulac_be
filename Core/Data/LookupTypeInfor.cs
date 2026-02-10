using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Data
{
    public sealed record LookupTypeInfo(
        short TypeId,
        string TypeCode,
        bool IsConfigurable
    )
    {
        public static readonly LookupTypeInfo AccountStatus =
            new(1, "ACCOUNT_STATUS", false);

        public static readonly LookupTypeInfo InventoryTxType =
            new(2, "INVENTORY_TX_TYPE", false);

        public static readonly LookupTypeInfo InventoryTxStatus =
            new(3, "INVENTORY_TX_STATUS", false);

        public static readonly LookupTypeInfo MediaType =
            new(4, "MEDIA_TYPE", true);

        public static readonly LookupTypeInfo TableStatus =
            new(5, "TABLE_STATUS", false);

        public static readonly LookupTypeInfo TableType =
            new(6, "TABLE_TYPE", true);

        public static readonly LookupTypeInfo ReservationSource =
            new(7, "RESERVATION_SOURCE", true);

        public static readonly LookupTypeInfo ReservationStatus =
            new(8, "RESERVATION_STATUS", false);

        public static readonly LookupTypeInfo OrderSource =
            new(9, "ORDER_SOURCE", true);

        public static readonly LookupTypeInfo OrderStatus =
            new(10, "ORDER_STATUS", false);

        public static readonly LookupTypeInfo PaymentMethod =
            new(11, "PAYMENT_METHOD", true);

        public static readonly LookupTypeInfo DishStatus =
            new(12, "DISH_STATUS", false);

        public static readonly LookupTypeInfo OrderItemStatus =
            new(13, "ORDER_ITEM_STATUS", false);

        public static readonly LookupTypeInfo Severity =
            new(14, "SEVERITY", true);

        public static readonly LookupTypeInfo PromotionType =
            new(15, "PROMOTION_TYPE", true);

        public static readonly LookupTypeInfo PromotionStatus =
            new(16, "PROMOTION_STATUS", false);

        public static readonly LookupTypeInfo IngredientType =
            new(17, "INGREDIENT_TYPE", true);

        public static readonly LookupTypeInfo Tag =
            new(18, "Tag", true);

        public static readonly LookupTypeInfo TableZone =
            new(19  , "Table_Zone", true);

        // Convenience: list all
        public static IReadOnlyList<LookupTypeInfo> All { get; } = new[]
        {
        AccountStatus, InventoryTxType, InventoryTxStatus, MediaType, TableStatus,
        TableType, ReservationSource, ReservationStatus, OrderSource, OrderStatus,
        PaymentMethod, DishStatus, OrderItemStatus, Severity, PromotionType,
        PromotionStatus, IngredientType
    };
    }

}
