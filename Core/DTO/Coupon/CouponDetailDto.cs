namespace Core.DTO.Coupon;

/// <summary>
/// Coupon detail response DTO
/// </summary>
public class CouponDetailDto
{
    public long CouponId { get; set; }
    public string CouponCode { get; set; } = string.Empty;
    public string CouponName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public decimal DiscountValue { get; set; }
    public int? MaxUsage { get; set; }
    public int? UsedCount { get; set; }
    public string Type { get; set; } = string.Empty;
    public string CouponStatus { get; set; } = string.Empty;
    public DateTime? CreatedAt { get; set; }
}
