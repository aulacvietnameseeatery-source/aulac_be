namespace Core.DTO.Loyalty;

public class LoyaltyCouponHistoryDto
{
    public long CouponId { get; set; }
    public long? CustomerId { get; set; }
    public string CouponCode { get; set; } = string.Empty;
    public string CouponName { get; set; } = string.Empty;
    public decimal DiscountValue { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int? MaxUsage { get; set; }
    public int? UsedCount { get; set; }
    public string CouponStatus { get; set; } = string.Empty;
    public string RedemptionStatus { get; set; } = string.Empty;
    public bool IsExpired { get; set; }
    public bool IsUsed { get; set; }
}