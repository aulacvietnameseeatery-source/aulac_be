using Core.DTO.General;
using Core.DTO.Payment;
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
    }
}
