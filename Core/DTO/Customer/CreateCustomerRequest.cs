using System.ComponentModel.DataAnnotations;

namespace Core.DTO.Customer;

public class CreateCustomerRequest
{
    [Required(ErrorMessage = "Phone number is required")]
    [MaxLength(20, ErrorMessage = "Phone cannot exceed 20 characters")]
    [RegularExpression(@"^\d+$", ErrorMessage = "Phone number must contain only digits")]
    public string Phone { get; set; } = null!;

    [MaxLength(100, ErrorMessage = "Full name cannot exceed 100 characters")]
    public string? FullName { get; set; }

    [MaxLength(150, ErrorMessage = "Email cannot exceed 150 characters")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string? Email { get; set; }

    public bool IsMember { get; set; } = false;

    [Range(0, int.MaxValue, ErrorMessage = "Loyalty points cannot be negative")]
    public int LoyaltyPoints { get; set; } = 0;
}
