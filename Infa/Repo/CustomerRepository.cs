using Core.DTO.Customer;
using Core.DTO.General;
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
    public class CustomerRepository : ICustomerRepository
    {
        private readonly RestaurantMgmtContext _context;

        public CustomerRepository(RestaurantMgmtContext context)
        {
            _context = context;
        }

        public async Task<PagedResultDTO<CustomerListDTO>> GetCustomersAsync(
            CustomerListQueryDTO query,
            CancellationToken ct = default)
        {
            var queryable = _context.Customers.AsQueryable();

            // ===== SEARCH =====

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.Trim().ToLower();

                queryable = queryable.Where(c =>
                    (c.FullName != null && c.FullName.ToLower().Contains(search)) ||
                    c.Phone.Contains(search) ||
                    (c.Email != null && c.Email.ToLower().Contains(search)));
            }

            if (query.IsMember.HasValue)
            {
                queryable = queryable.Where(c =>
                    c.IsMember == query.IsMember);
            }

            if (query.FromDate.HasValue)
            {
                queryable = queryable.Where(c =>
                    c.CreatedAt >= query.FromDate.Value);
            }

            if (query.ToDate.HasValue)
            {
                queryable = queryable.Where(c =>
                    c.CreatedAt <= query.ToDate.Value);
            }

            var totalCount = await queryable.CountAsync(ct);

            var pageIndex = query.PageIndex < 1 ? 1 : query.PageIndex;
            var pageSize = query.PageSize < 1 ? 10 : query.PageSize;

            var customers = await queryable
                .OrderByDescending(c => c.CustomerId)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new CustomerListDTO
                {
                    CustomerId = c.CustomerId,
                    FullName = c.FullName,
                    Phone = c.Phone,
                    Email = c.Email,
                    IsMember = c.IsMember,
                    LoyaltyPoints = c.LoyaltyPoints,
                    CreatedAt = c.CreatedAt,

                    OrderCount = c.Orders.Count(),
                    ReservationCount = c.Reservations.Count(),

                    LastOrderTime = c.Orders
                        .OrderByDescending(o => o.CreatedAt)
                        .Select(o => (DateTime?)o.CreatedAt)
                        .FirstOrDefault()
                })
                .ToListAsync(ct);

            return new PagedResultDTO<CustomerListDTO>
            {
                PageData = customers,
                TotalCount = totalCount,
                PageIndex = pageIndex,
                PageSize = pageSize
            };
        }

        public async Task<Customer?> GetByIdAsync(long id, CancellationToken ct = default)
        {
            return await _context.Customers
                .FirstOrDefaultAsync(c => c.CustomerId == id, ct);
        }

        public async Task<Customer?> GetByPhoneAsync(string phone)
        {
            return await _context.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Phone == phone);
        }

        public async Task UpdateAsync(Customer customer, CancellationToken ct = default)
        {
            _context.Customers.Update(customer);
            await _context.SaveChangesAsync(ct);
        }

        public async Task AddAsync(Customer customer, CancellationToken ct = default)
        {
            await _context.Customers.AddAsync(customer, ct);
            await _context.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(Customer customer, CancellationToken ct = default)
        {
            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync(ct);
        }

        public async Task<bool> HasOrdersOrReservationsAsync(long customerId, CancellationToken ct = default)
        {
            var hasOrders = await _context.Orders.AnyAsync(o => o.CustomerId == customerId, ct);
            if (hasOrders) return true;

            var hasReservations = await _context.Reservations.AnyAsync(r => r.CustomerId == customerId, ct);
            return hasReservations;
        }

        public async Task<Customer> FindOrCreateAsync(string phone, string? fullName, string? email, CancellationToken ct = default)
        {
            // Try to find by phone first (unique key)
            var existing = await _context.Customers
                .FirstOrDefaultAsync(c => c.Phone == phone, ct);

            if (existing != null)
            {
                bool updated = false;
                if (!string.IsNullOrWhiteSpace(fullName) && existing.FullName != fullName.Trim())
                {
                    existing.FullName = fullName.Trim();
                    updated = true;
                }
                if (!string.IsNullOrWhiteSpace(email) && existing.Email != email.Trim())
                {
                    existing.Email = email.Trim();
                    updated = true;
                }

                if (updated)
                {
                    await _context.SaveChangesAsync(ct);
                }

                return existing;
            }

            // Create new customer
            var newCustomer = new Customer
            {
                Phone = phone,
                FullName = string.IsNullOrWhiteSpace(fullName) ? null : fullName.Trim(),
                Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
                IsMember = false,
                LoyaltyPoints = 0,
                CreatedAt = DateTime.UtcNow
            };

            _context.Customers.Add(newCustomer);
            await _context.SaveChangesAsync(ct);

            return newCustomer;
        }

        public async Task<CustomerDetailDTO?> GetCustomerDetailAsync(
            long customerId,
            CancellationToken ct = default)
        {
            return await _context.Customers
                .Where(c => c.CustomerId == customerId)
                .Select(c => new CustomerDetailDTO
                {
                    CustomerId = c.CustomerId,
                    FullName = c.FullName,
                    Phone = c.Phone,
                    Email = c.Email,
                    IsMember = c.IsMember,
                    LoyaltyPoints = c.LoyaltyPoints,
                    CreatedAt = c.CreatedAt,

                    OrderCount = c.Orders.Count(),
                    ReservationCount = c.Reservations.Count(),

                    LastOrderTime = c.Orders
                        .OrderByDescending(o => o.CreatedAt)
                        .Select(o => (DateTime?)o.CreatedAt)
                        .FirstOrDefault(),

                    TotalSpent = c.Orders
                        .Where(o => o.OrderStatusLv.ValueCode == OrderStatusCode.COMPLETED.ToString())
                        .Sum(o => (decimal?)o.TotalAmount) ?? 0
                })
                .FirstOrDefaultAsync(ct);
        }

        public async Task<PagedResultDTO<CustomerOrderDTO>> GetCustomerOrdersAsync(
            CustomerOrderQueryDTO query,
            CancellationToken ct = default)
        {
            var queryable = _context.Orders
                .Where(o => o.CustomerId == query.CustomerId)
                .AsQueryable();

            if (query.FromDate.HasValue)
            {
                queryable = queryable.Where(o =>
                    o.CreatedAt >= query.FromDate);
            }

            if (query.ToDate.HasValue)
            {
                queryable = queryable.Where(o =>
                    o.CreatedAt <= query.ToDate);
            }
            if (query.OrderType.HasValue)
            {
                var statusCode = query.OrderType.Value.ToString();
                queryable = queryable.Where(o => o.SourceLv.ValueCode == statusCode);
            }else if (query.OrderTypeLvId.HasValue)
            {
                queryable = queryable.Where(o =>
                    o.SourceLvId == query.OrderTypeLvId);
            }

            var totalCount = await queryable.CountAsync(ct);

            var pageIndex = query.PageIndex < 1 ? 1 : query.PageIndex;
            var pageSize = query.PageSize < 1 ? 10 : query.PageSize;

            var orders = await queryable
                .OrderByDescending(o => o.CreatedAt)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new CustomerOrderDTO
                {
                    OrderId = o.OrderId,
                    CreatedAt = o.CreatedAt,
                    TotalAmount = o.TotalAmount,
                    TipAmount = o.TipAmount,

                    OrderType = o.SourceLv.ValueCode,
                    Status = o.OrderStatusLv.ValueCode
                })
                .ToListAsync(ct);

            return new PagedResultDTO<CustomerOrderDTO>
            {
                PageData = orders,
                TotalCount = totalCount,
                PageIndex = pageIndex,
                PageSize = pageSize
            };
        }

        public async Task<CustomerOrderDetailDTO?> GetCustomerOrderDetailAsync(
    long customerId,
    long orderId,
    CancellationToken ct = default)
        {
            return await _context.Orders
                .Where(o => o.OrderId == orderId && o.CustomerId == customerId)
                .Select(o => new CustomerOrderDetailDTO
                {
                    OrderId = o.OrderId,
                    CreatedAt = o.CreatedAt,
                    TotalAmount = o.TotalAmount,
                    TipAmount = o.TipAmount,

                    Status = o.OrderStatusLv.ValueCode,
                    OrderType = o.SourceLv.ValueCode,

                    TableCode = o.Table != null ? o.Table.TableCode : null,

                    StaffName = o.Staff != null ? o.Staff.FullName : null,

                    Items = o.OrderItems.Select(i => new CustomerOrderItemDTO
                    {
                        OrderItemId = i.OrderItemId,
                        DishId = i.DishId,
                        DishName = i.Dish.DishName,
                        Quantity = i.Quantity,
                        Price = i.Price,
                        Status = i.ItemStatusLv.ValueCode,
                        Note = i.Note
                    }).ToList(),

                    Promotions = o.OrderPromotions.Select(p => new CustomerOrderPromotionDTO
                    {
                        PromotionId = p.PromotionId,
                        PromotionName = p.Promotion.PromoName,
                        DiscountAmount = p.DiscountAmount
                    }).ToList(),

                    Coupons = o.OrderCoupons.Select(c => new CustomerOrderCouponDTO
                    {
                        CouponId = c.CouponId,
                        CouponCode = c.Coupon.CouponCode,
                        DiscountAmount = c.DiscountAmount
                    }).ToList(),

                    Payments = o.Payments.Select(p => new CustomerOrderPaymentDTO
                    {
                        PaymentId = p.PaymentId,
                        ReceivedAmount = p.ReceivedAmount,
                        ChangeAmount = p.ChangeAmount,
                        PaidAt = p.PaidAt,
                        Method = p.MethodLv.ValueCode
                    }).ToList()
                })
                .FirstOrDefaultAsync(ct);
        }
    }
}
