using System;
using System.Collections.Generic;

namespace Core.Entity;

public partial class Role
{
    public long RoleId { get; set; }

    public string RoleCode { get; set; } = null!;

    public string RoleName { get; set; } = null!;

    public virtual ICollection<StaffAccount> StaffAccounts { get; set; } = new List<StaffAccount>();

    public uint RoleStatusLvId { get; set; }

    public virtual LookupValue? RoleStatusLv { get; set; }

    public virtual ICollection<Permission> Permissions { get; set; } = new List<Permission>();
}
