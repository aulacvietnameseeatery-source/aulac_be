using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Enum
{
    public enum LookupType : ushort
    {
        AccountStatus = 1,  // ACCOUNT_STATUS
        InventoryTxType = 2,  // INVENTORY_TX_TYPE
        InventoryTxStatus = 3,  // INVENTORY_TX_STATUS
        MediaType = 4,  // MEDIA_TYPE
        TableStatus = 5,  // TABLE_STATUS
        TableType = 6,  // TABLE_TYPE
        ReservationSource = 7,  // RESERVATION_SOURCE
        ReservationStatus = 8,  // RESERVATION_STATUS
        OrderSource = 9,  // ORDER_SOURCE
        OrderStatus = 10, // ORDER_STATUS
        PaymentMethod = 11, // PAYMENT_METHOD
        DishStatus = 12, // DISH_STATUS
        OrderItemStatus = 13, // ORDER_ITEM_STATUS
        Severity = 14, // SEVERITY
        PromotionType = 15, // PROMOTION_TYPE
        PromotionStatus = 16, // PROMOTION_STATUS
        IngredientType = 17,  // INGREDIENT_TYPE
        Tag = 18,         // TAG
        TableZone = 19,   // TABLE_ZONE
    }
}
