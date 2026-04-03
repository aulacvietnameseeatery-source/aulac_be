using Core.DTO.Loyalty;

namespace Core.Interface.Service.Loyalty;

public interface ILoyaltyService
{
    Task<LoyaltyExchangeResultDto> ExchangePointsToCouponAsync(LoyaltyExchangeRequest request, CancellationToken ct = default);

    Task<List<LoyaltyCouponHistoryDto>> GetCustomerCouponsAsync(long customerId, CancellationToken ct = default);
}