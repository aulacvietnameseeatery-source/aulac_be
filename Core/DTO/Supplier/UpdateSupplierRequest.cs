using System.ComponentModel.DataAnnotations;

namespace Core.DTO.Supplier;

/// <summary>
/// Request DTO for updating an existing supplier
/// </summary>
public class UpdateSupplierRequest
{
    [Required(ErrorMessage = "Supplier name is required")]
    [MaxLength(200, ErrorMessage = "Supplier name cannot exceed 200 characters")]
    public string SupplierName { get; set; } = string.Empty;

    [MaxLength(50, ErrorMessage = "Phone cannot exceed 50 characters")]
    [RegularExpression(@"^\d+$", ErrorMessage = "Phone number must contain only digits")]
    public string? Phone { get; set; }

    [MaxLength(150, ErrorMessage = "Email cannot exceed 150 characters")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string? Email { get; set; }

    /// <summary>
    /// List of ingredient IDs that this supplier provides
    /// </summary>
    public List<long> IngredientIds { get; set; } = new();
}
