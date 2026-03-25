using System.ComponentModel.DataAnnotations;

namespace Core.DTO.Inventory;

/// <summary>
/// Request to approve or reject a PENDING_APPROVAL transaction.
/// </summary>
public class ApproveTransactionRequest
{
    /// <summary>
    /// true = approve (→ COMPLETED), false = reject (→ CANCELLED)
    /// </summary>
    [Required]
    public bool IsApproved { get; set; }

    /// <summary>
    /// Required when rejecting. Optional approval note.
    /// </summary>
    [MaxLength(500)]
    public string? Note { get; set; }
}
