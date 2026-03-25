namespace Core.DTO.Inventory;

/// <summary>
/// DTO for media (photo evidence) attached to a transaction.
/// </summary>
public class TransactionMediaDto
{
    public long MediaId { get; set; }
    public string? Url { get; set; }
    public string? MediaType { get; set; }
}
