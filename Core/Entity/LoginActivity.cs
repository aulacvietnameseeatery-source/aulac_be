using System;

namespace Core.Entity;

/// <summary>
/// Immutable login activity event for auditable system access history.
/// Separate from AuthSession which is mutable and may be cleaned up.
/// </summary>
public partial class LoginActivity
{
    public long LoginActivityId { get; set; }

    /// <summary>FK ? staff_account.account_id.</summary>
    public long StaffId { get; set; }

    /// <summary>Optional link to auth_session.session_id.</summary>
    public long? SessionId { get; set; }

    /// <summary>LOGIN, LOGOUT, TOKEN_REFRESH, FORCE_LOGOUT.</summary>
    public string EventType { get; set; } = null!;

    public string? DeviceInfo { get; set; }

    public string? IpAddress { get; set; }

    public DateTime OccurredAt { get; set; }

    // ?? Navigation ??

    public virtual StaffAccount Staff { get; set; } = null!;

    public virtual AuthSession? Session { get; set; }
}
