using System.ComponentModel.DataAnnotations;
using Core.Extensions;

namespace Core.DTO.Customer;

public class UpdateCustomerRequest
{
    [Required(ErrorMessage = "Phone number is required")]
    [StringLength(15, ErrorMessage = "Phone number cannot exceed 15 characters")]
    [RegularExpression(PhoneNumberExtensions.SupportedPhoneValidationPattern, ErrorMessage = "Invalid phone number format")]
    public string Phone { get; set; } = null!;

    [MaxLength(100, ErrorMessage = "Full name cannot exceed 100 characters")]
    public string? FullName { get; set; }

    [MaxLength(150, ErrorMessage = "Email cannot exceed 150 characters")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string? Email { get; set; }

    public bool IsMember { get; set; } = false;
}
