using System;
using System.Collections.Generic;

namespace Core.Entity;

public partial class AuthSession
{
    public long SessionId { get; set; }

    public long UserId { get; set; }

    public string TokenHash { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    public DateTime? CreatedAt { get; set; }

    public bool? Revoked { get; set; }

    public virtual Account User { get; set; } = null!;
}
