using System;
using System.Collections.Generic;

namespace Core.Entity;

public partial class StaffAccount
{
    public long AccountId { get; set; }

    public string FullName { get; set; } = null!;

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public long RoleId { get; set; }

    public DateTime CreatedAt { get; set; }

    public string Username { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public bool IsLocked { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public uint AccountStatusLvId { get; set; }

    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    public virtual ICollection<AuthSession> AuthSessions { get; set; } = new List<AuthSession>();

    public virtual ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual Role Role { get; set; } = null!;

    public virtual ICollection<ServiceError> ServiceErrorResolvedByNavigations { get; set; } = new List<ServiceError>();

    public virtual ICollection<ServiceError> ServiceErrorStaffs { get; set; } = new List<ServiceError>();

    public virtual ICollection<SystemSetting> SystemSettings { get; set; } = new List<SystemSetting>();
}
