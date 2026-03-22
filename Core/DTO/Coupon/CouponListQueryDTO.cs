namespace Core.DTO.Coupon;

/// <summary>
/// Coupon list query parameters
/// </summary>
public class CouponListQueryDTO
{
    /// <summary>
    /// Search by coupon code or coupon name
    /// </summary>
    public string? Search { get; set; }

    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
