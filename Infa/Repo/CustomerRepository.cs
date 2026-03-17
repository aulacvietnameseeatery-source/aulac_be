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

        public async Task<Customer> FindOrCreateAsync(string phone, string? fullName, string? email, CancellationToken ct = default)
        {
            // Try to find by phone first (unique key)
            var existing = await _context.Customers
                .FirstOrDefaultAsync(c => c.Phone == phone, ct);

            if (existing != null)
                return existing;

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
    }
}
