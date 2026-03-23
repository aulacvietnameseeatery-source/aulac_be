namespace Core.DTO.Inventory;

/// <summary>
/// Request to submit a DRAFT transaction for approval (DRAFT → PENDING_APPROVAL).
/// Optionally attach media IDs (photo evidence) before submission.
/// </summary>
public class SubmitTransactionRequest
{
    /// <summary>
    /// Optional media asset IDs to attach as evidence before submitting.
    /// </summary>
    public List<long>? MediaIds { get; set; }
}
