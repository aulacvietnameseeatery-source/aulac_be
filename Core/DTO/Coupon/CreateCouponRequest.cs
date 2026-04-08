using System.ComponentModel.DataAnnotations;

namespace Core.DTO.Coupon;

/// <summary>
/// Request DTO for creating a new coupon
/// </summary>
public class CreateCouponRequest
{
    [Required(ErrorMessage = "Coupon code is required")]
    [MaxLength(50, ErrorMessage = "Coupon code cannot exceed 50 characters")]
    public string CouponCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Coupon name is required")]
    [MaxLength(200, ErrorMessage = "Coupon name cannot exceed 200 characters")]
    public string CouponName { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Start time is required")]
    public DateTime StartTime { get; set; }

    [Required(ErrorMessage = "End time is required")]
    public DateTime EndTime { get; set; }

    [Required(ErrorMessage = "Discount value is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Discount value must be greater than 0")]
    public decimal DiscountValue { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Max usage must be greater than 0")]
    public int? MaxUsage { get; set; }

    [Required(ErrorMessage = "Type is required")]
    [RegularExpression("^(FIXED_AMOUNT|PERCENT)$", ErrorMessage = "Type must be either FIXED_AMOUNT or PERCENT")]
    public string Type { get; set; } = string.Empty;
}
