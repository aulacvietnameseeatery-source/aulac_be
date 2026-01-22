using System;
using System.Collections.Generic;

namespace Core.Entity;

public partial class Role
{
    public long RoleId { get; set; }

    public string RoleCode { get; set; } = null!;

    public string RoleName { get; set; } = null!;

    public virtual ICollection<Account> Accounts { get; set; } = new List<Account>();

    public virtual ICollection<Permission> Permissions { get; set; } = new List<Permission>();
}
