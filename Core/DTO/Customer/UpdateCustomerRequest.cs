using System.ComponentModel.DataAnnotations;

namespace Core.DTO.Customer;

public class UpdateCustomerRequest
{
    [Required(ErrorMessage = "Phone number is required")]
    [StringLength(13, MinimumLength = 10, ErrorMessage = "Phone number must be between 10 and 13 characters")]
    [RegularExpression(@"^\d+$", ErrorMessage = "Phone number must contain only digits")]
    public string Phone { get; set; } = null!;

    [MaxLength(100, ErrorMessage = "Full name cannot exceed 100 characters")]
    public string? FullName { get; set; }

    [MaxLength(150, ErrorMessage = "Email cannot exceed 150 characters")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string? Email { get; set; }

    public bool IsMember { get; set; } = false;
}
