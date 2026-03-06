namespace Core.DTO.Supplier;

/// <summary>
/// Supplier list query parameters
/// </summary>
public class SupplierListQueryDTO
{
    /// <summary>
    /// Search by supplier name, phone, or email
    /// </summary>
    public string? Search { get; set; }

    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
