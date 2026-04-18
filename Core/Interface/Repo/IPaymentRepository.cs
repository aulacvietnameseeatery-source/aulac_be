using Core.DTO.General;
using Core.DTO.Payment;
using Core.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interface.Repo
{
    public interface IPaymentRepository
    {
        Task<PagedResultDTO<PaymentListDTO>> GetPaymentsAsync(PaymentListQueryDTO query, CancellationToken ct);
        Task<Order?> GetOrderForPaymentAsync(long orderId, CancellationToken ct);
        Task<Coupon?> GetCouponWithTypeAsync(long couponId, CancellationToken ct);
        Task<List<Promotion>> GetActivePromotionsAsync(uint activePromotionStatusId, DateTime now, CancellationToken ct);
        Task<RestaurantTable?> GetTableByIdAsync(long tableId, CancellationToken ct);
        Task AddPaymentAsync(Payment payment, CancellationToken ct);
    }
}
