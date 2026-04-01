namespace Core.DTO.Account;

/// <summary>
/// Inventory-transaction row shown in the account detail → Inventory tab.
/// Includes transactions the staff created or approved.
/// </summary>
public class AccountInventoryActivityDTO
{
    public long TransactionId { get; set; }
    public string? TransactionCode { get; set; }
    public string TypeName { get; set; } = "";
    public string StatusName { get; set; } = "";
    public string? Note { get; set; }
    public DateTime? CreatedAt { get; set; }

    /// <summary>"Creator" or "Approver" — describes the staff's role in this transaction.</summary>
    public string StaffRole { get; set; } = "";

    public int ItemCount { get; set; }
}
