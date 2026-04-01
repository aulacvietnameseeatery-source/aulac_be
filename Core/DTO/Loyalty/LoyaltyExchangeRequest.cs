using System.ComponentModel.DataAnnotations;

namespace Core.DTO.Loyalty;

public class LoyaltyExchangeRequest
{
    [Required]
    public long CustomerId { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Points must be greater than 0")]
    public int Points { get; set; }
}