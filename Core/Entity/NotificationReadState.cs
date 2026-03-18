using System;

namespace Core.Entity;

public partial class NotificationReadState
{
    public long NotificationReadStateId { get; set; }

    public long NotificationId { get; set; }

    public long UserId { get; set; }

    public bool IsRead { get; set; }

    public bool IsAcknowledged { get; set; }

    public DateTime? ReadAt { get; set; }

    public DateTime? AcknowledgedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Notification Notification { get; set; } = null!;
}
