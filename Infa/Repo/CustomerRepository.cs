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

        public async Task<Customer?> GetByPhoneAsync(string phone)
        {
            return await _context.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Phone == phone);
        }
    }
}
