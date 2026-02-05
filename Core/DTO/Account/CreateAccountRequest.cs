using System.ComponentModel.DataAnnotations;

namespace Core.DTO.Account;

/// <summary>
/// Request DTO for creating a new user account.
/// </summary>
public record CreateAccountRequest
{
    /// <summary>
    /// Email address (required, unique).
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [StringLength(150, ErrorMessage = "Email cannot exceed 150 characters")]
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// Full name of the user (required).
    /// </summary>
    [Required(ErrorMessage = "Full name is required")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 150 characters")]
    public string FullName { get; init; } = string.Empty;

    /// <summary>
    /// Phone number (optional).
    /// </summary>
    [RegularExpression(@"^(0|\+84)[0-9]{9,10}$", ErrorMessage = "Invalid Vietnamese phone number format")]
    [StringLength(30, ErrorMessage = "Phone cannot exceed 30 characters")]
    public string? Phone { get; init; }

    /// <summary>
    /// Role ID (required).
    /// </summary>
    [Required(ErrorMessage = "Role is required")]
    [Range(1, long.MaxValue, ErrorMessage = "Invalid role ID")]
    public long RoleId { get; init; }
}
