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
    }
}
