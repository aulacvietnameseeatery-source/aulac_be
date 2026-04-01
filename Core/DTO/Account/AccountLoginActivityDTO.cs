namespace Core.DTO.Account;

/// <summary>
/// Login-activity row shown in the account detail → Security / Login Activity tab.
/// </summary>
public class AccountLoginActivityDTO
{
    public long LoginActivityId { get; set; }
    public string EventType { get; set; } = null!;
    public string? DeviceInfo { get; set; }
    public string? IpAddress { get; set; }
    public DateTime OccurredAt { get; set; }
}
