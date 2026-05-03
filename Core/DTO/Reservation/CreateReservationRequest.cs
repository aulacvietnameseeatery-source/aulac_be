using System.ComponentModel.DataAnnotations;
using Core.Extensions;

namespace Core.DTO.Reservation;

/// <summary>
/// Request DTO for submitting a final reservation.
/// </summary>
public class CreateReservationRequest : IValidatableObject
{
    public int? CustomerId { get; set; }

    [StringLength(150, MinimumLength = 2)]
    public string? CustomerName { get; set; }

    [Required]
    [StringLength(15)]
    [RegularExpression(PhoneNumberExtensions.SupportedPhoneValidationPattern, ErrorMessage = "Invalid phone number format")]
    public string Phone { get; set; } = null!;

    [EmailAddress]
    [StringLength(150)]
    public string? Email { get; set; }

    [Required]
    [Range(1, 50)]
    public int PartySize { get; set; }

    [Required]
    public DateTime ReservedTime { get; set; }

    [StringLength(512)]
    public string? BookingToken { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if ((!CustomerId.HasValue || CustomerId.Value <= 0) && string.IsNullOrWhiteSpace(CustomerName))
        {
            yield return new ValidationResult(
                "Customer name is required.",
                new[] { nameof(CustomerName) });
        }
    }
}
