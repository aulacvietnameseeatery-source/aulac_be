using System.ComponentModel.DataAnnotations;

namespace Core.DTO.Account;

/// <summary>
/// Request DTO for changing password.
/// Supports both first-time password change and normal password change.
/// </summary>
public record ChangePasswordRequest
{
    /// <summary>
    /// Current password (required for normal password change, not required for first-time change).
    /// </summary>
    [StringLength(255, ErrorMessage = "Current password cannot exceed 255 characters")]
    public string? CurrentPassword { get; init; }

    /// <summary>
    /// New password (required).
    /// </summary>
    [Required(ErrorMessage = "New password is required")]
    [StringLength(128, MinimumLength = 8, ErrorMessage = "New password must be between 8 and 128 characters")]
    public string NewPassword { get; init; } = string.Empty;

    /// <summary>
    /// Confirm new password (required, must match NewPassword).
    /// </summary>
    [Required(ErrorMessage = "Password confirmation is required")]
    [Compare(nameof(NewPassword), ErrorMessage = "New password and confirmation do not match")]
    public string ConfirmPassword { get; init; } = string.Empty;
}
