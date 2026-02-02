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
        public const string DeleteAccount = "ACCOUNT:DELETE";
        public const string ResetPassword = "ACCOUNT:RESET_PASSWORD";

        //System Settings
        public const string ViewSystemSettings = "SYSTEM_SETTING:READ";
        public const string ManageSystemSettings = "SYSTEM_SETTING:EDIT";

        //...
    }
}
