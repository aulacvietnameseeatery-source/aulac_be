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
