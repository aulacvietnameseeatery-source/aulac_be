namespace Core.DTO.Loyalty;

public class LoyaltyExchangeResultDto
{
    public long CustomerId { get; set; }
    public int SpentPoints { get; set; }
    public int RemainingPoints { get; set; }
    public long CouponId { get; set; }
    public string CouponCode { get; set; } = string.Empty;
    public string CouponName { get; set; } = string.Empty;
    public decimal DiscountValue { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}