using System.ComponentModel.DataAnnotations;
using Core.Extensions;

namespace Core.DTO.Account;

/// <summary>
/// Request DTO for updating account profile information.
/// </summary>
public record UpdateAccountRequest
{
    /// <summary>
    /// Email address (optional, nullable means no change).
    /// If provided, must be unique.
    /// </summary>
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [StringLength(150, ErrorMessage = "Email cannot exceed 150 characters")]
    public string? Email { get; init; }

    /// <summary>
    /// Full name (optional, nullable means no change).
    /// </summary>
    [StringLength(150, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 150 characters")]
    public string? FullName { get; init; }

    /// <summary>
    /// Phone number (optional, null to clear phone).
    /// </summary>
    [RegularExpression(PhoneNumberExtensions.SupportedPhoneValidationPattern, ErrorMessage = "Invalid phone number format")]
    [StringLength(30, ErrorMessage = "Phone cannot exceed 30 characters")]
    public string? Phone { get; init; }

    /// <summary>
    /// Role ID (optional, admin-only).
    /// Nullable means no change.
    /// </summary>
    [Range(1, long.MaxValue, ErrorMessage = "Invalid role ID")]
    public long? RoleId { get; init; }
}
