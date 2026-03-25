namespace Core.DTO.Supplier;

/// <summary>
/// Supplier response DTO
/// </summary>
public class SupplierDto
{
    public long SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? TaxCode { get; set; }
}
