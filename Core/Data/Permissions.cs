using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Data
{
    public static class Permissions
    {
        //Account
        public const string ViewAccount = "ACCOUNT:READ";
        public const string CreateAccount = "ACCOUNT:CREATE";
        public const string EditAccount = "ACCOUNT:EDIT";
        public const string UpdateAccount = "ACCOUNT:UPDATE";
        public const string DeleteAccount = "ACCOUNT:DELETE";
        public const string ResetPassword = "ACCOUNT:RESET_PASSWORD";

        //System Settings
        public const string ViewSystemSettings = "SYSTEM_SETTING:READ";
        public const string ManageSystemSettings = "SYSTEM_SETTING:EDIT";

        //Dish
        public const string ViewDish = "DISH:READ";
        public const string CreateDish = "DISH:CREATE";
        public const string EditDish = "DISH:EDIT";
        public const string DeleteDish = "DISH:DELETE";


        //Dish Category
        public const string ViewDishCategory = "DISH_CATEGORY:READ";
        public const string CreateDishCategory = "DISH_CATEGORY:CREATE";
        public const string EditDishCategory = "DISH_CATEGORY:EDIT";
        public const string DeleteDishCategory = "DISH_CATEGORY:DELETE";

        //Supplier
        public const string ViewSupplier = "SUPPLIER:READ";
        public const string CreateSupplier = "SUPPLIER:CREATE";
        public const string EditSupplier = "SUPPLIER:EDIT";
        public const string DeleteSupplier = "SUPPLIER:DELETE";

        //Coupon
        public const string ViewCoupon = "COUPON:READ";
        public const string CreateCoupon = "COUPON:CREATE";
        public const string EditCoupon = "COUPON:EDIT";
        public const string DeleteCoupon = "COUPON:DELETE";

        //Role
        public const string ViewRole = "ROLE:READ";
        public const string CreateRole = "ROLE:CREATE";
        public const string UpdateRole = "ROLE:UPDATE";
        public const string DeleteRole = "ROLE:DELETE";


        //Reservatione
        public const string ViewReservation = "RESERVATION:READ";
        public const string CreateReservation = "RESERVATION:CREATE";
        public const string UpdateReservation = "RESERVATION:UPDATE";
        public const string DeleteReservation = "RESERVATION:DELETE";

        //Order
        public const string ViewOrder = "ORDER:READ";
        public const string CreateOrder = "ORDER:CREATE";
        public const string EditOrder = "ORDER:EDIT";
        public const string UpdateOrderItemStatus = "ORDER:UPDATE_ITEM_STATUS";
        public const string ProcessPayment = "ORDER:PROCESS_PAYMENT";

        //Table
        public const string ViewTable = "TABLE:READ";
        public const string CreateTable = "TABLE:CREATE";
        public const string UpdateTable = "TABLE:EDIT";
        public const string DeleteTable = "TABLE:DELETE";
        public const string UpdateTableStatus = "TABLE:UPDATE_STATUS";
        public const string ManageTableZone = "TABLE:MANAGE_ZONE";
        public const string ManageTableType = "TABLE:MANAGE_TYPE";
        public const string ManageTableMedia = "TABLE:MANAGE_MEDIA";

        //Promotion
        public const string ViewPromotion = "PROMOTION:READ";
        public const string CreatePromotion = "PROMOTION:CREATE";
        public const string UpdatePromotion = "PROMOTION:UPDATE";

        // Shift
        public const string ViewShift = "SHIFT:READ";
        public const string ViewOwnShift = "SHIFT:READ_OWN";
        public const string ScheduleShift = "SHIFT:SCHEDULE";
        public const string AssignShift = "SHIFT:ASSIGN";
        public const string CheckInShift = "SHIFT:CHECK_IN";
        public const string CheckOutShift = "SHIFT:CHECK_OUT";
        public const string AdjustAttendance = "SHIFT:ADJUST_ATTENDANCE";
        public const string ViewShiftReport = "SHIFT:REPORT_READ";
        public const string CloseShift = "SHIFT:CLOSE";
        public const string ManageShiftTemplate = "SHIFT:MANAGE_TEMPLATE";
        public const string PublishShift = "SHIFT:PUBLISH";

        public const string ViewCustomer = "CUSTOMER:READ";
        public const string CreateCustomer = "CUSTOMER:CREATE";
        public const string UpdateCustomer = "CUSTOMER:UPDATE";
        public const string DeleteCustomer = "CUSTOMER:DELETE";

        // Notification
        public const string ViewNotification = "NOTIFICATION:READ";
        public const string AcknowledgeNotification = "NOTIFICATION:ACK";

        // Inventory
        public const string ViewInventory = "INVENTORY:READ";
        public const string CreateInventoryTx = "INVENTORY:CREATE";
        public const string ApproveInventoryTx = "INVENTORY:APPROVE";
        public const string StockCheck = "INVENTORY:STOCK_CHECK";
        public const string ViewInventoryReport = "INVENTORY:REPORT_READ";

        //Payment
        public const string ViewPayment = "PAYMENT:READ";
    }
}