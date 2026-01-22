using System;
using System.Collections.Generic;

namespace Core.Entity;

public partial class Permission
{
    public long PermissionId { get; set; }

    public string ScreenCode { get; set; } = null!;

    public string ActionCode { get; set; } = null!;

    public virtual ICollection<Role> Roles { get; set; } = new List<Role>();
}
