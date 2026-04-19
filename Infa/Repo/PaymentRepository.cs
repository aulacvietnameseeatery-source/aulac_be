using Core.DTO.General;
using Core.DTO.Payment;
using Core.Entity;
using Core.Interface.Repo;
using Infa.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infa.Repo
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly RestaurantMgmtContext _context;

        public PaymentRepository(RestaurantMgmtContext context)
        {
            _context = context;
        }

        public async Task<PagedResultDTO<PaymentListDTO>> GetPaymentsAsync(
            PaymentListQueryDTO query,
            CancellationToken ct = default)
        {
            var queryable = _context.Payments
                .AsNoTracking()
                .Include(p => p.Order)
                    .ThenInclude(o => o.Customer)
                .Include(p => p.MethodLv)
                .AsQueryable();

            // ===== SEARCH =====
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.Trim().ToLower();

                queryable = queryable.Where(p =>
                    p.OrderId.ToString().Contains(search) ||
                    (p.Order.Customer != null &&
                        (
                            (p.Order.Customer.FullName != null &&
                             p.Order.Customer.FullName.ToLower().Contains(search)) ||
                            p.Order.Customer.Phone.Contains(search)
                        )
                    )
                );
            }

            // ===== FILTER METHOD =====
            if (query.Method.HasValue)
            {
                var methodCode = query.Method.Value.ToString();

                queryable = queryable.Where(p =>
                    p.MethodLv.ValueCode == methodCode);
            }

            // ===== FILTER DATE =====
            if (query.FromDate.HasValue)
            {
                queryable = queryable.Where(p =>
                    p.PaidAt >= query.FromDate.Value);
            }

            if (query.ToDate.HasValue)
            {
                queryable = queryable.Where(p =>
                    p.PaidAt <= query.ToDate.Value);
            }

            // ===== COUNT =====
            var totalCount = await queryable.CountAsync(ct);

            var pageIndex = query.PageIndex < 1 ? 1 : query.PageIndex;
            var pageSize = query.PageSize < 1 ? 10 : query.PageSize;

            // ===== DATA =====
            var payments = await queryable
                .OrderByDescending(p => p.PaymentId)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new PaymentListDTO
                {
                    PaymentId = p.PaymentId,
                    OrderId = p.OrderId,
                    ReceivedAmount = p.ReceivedAmount,
                    ChangeAmount = p.ChangeAmount,
                    FinalAmount = p.ReceivedAmount - p.ChangeAmount,

                    Method = p.MethodLv.ValueName,

                    PaidAt = p.PaidAt,

                    CustomerName = p.Order.Customer != null
                        ? p.Order.Customer.FullName
                        : null,

                    CustomerPhone = p.Order.Customer != null
                        ? p.Order.Customer.Phone
                        : null
                })
                .ToListAsync(ct);

            return new PagedResultDTO<PaymentListDTO>
            {
                PageData = payments,
                TotalCount = totalCount,
                PageIndex = pageIndex,
                PageSize = pageSize
            };
        }

        public Task<Order?> GetOrderForPaymentAsync(long orderId, CancellationToken ct)
        {
            return _context.Orders
                .Include(o => o.Payments)
                .Include(o => o.Customer)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Dish)
                .Include(o => o.OrderCoupons)
                .Include(o => o.OrderPromotions)
                .FirstOrDefaultAsync(o => o.OrderId == orderId, ct);
        }

        public Task<Coupon?> GetCouponWithTypeAsync(long couponId, CancellationToken ct)
        {
            return _context.Coupons
                .Include(c => c.TypeLv)
                .FirstOrDefaultAsync(c => c.CouponId == couponId, ct);
        }

        public Task<List<Promotion>> GetActivePromotionsAsync(uint activePromotionStatusId, DateTime now, CancellationToken ct)
        {
            return _context.Promotions
                .Include(p => p.TypeLv)
                .Include(p => p.PromotionRules)
                .Include(p => p.PromotionTargets)
                .Where(p => p.PromotionStatusLvId == activePromotionStatusId)
                .Where(p => now >= p.StartTime && now <= p.EndTime)
                .Where(p => !p.MaxUsage.HasValue || (p.UsedCount ?? 0) < p.MaxUsage.Value)
                .ToListAsync(ct);
        }

        public Task<RestaurantTable?> GetTableByIdAsync(long tableId, CancellationToken ct)
        {
            return _context.RestaurantTables
                .FirstOrDefaultAsync(t => t.TableId == tableId, ct);
        }

        public Task AddPaymentAsync(Payment payment, CancellationToken ct)
        {
            return _context.Payments.AddAsync(payment, ct).AsTask();
        }
    }
}
