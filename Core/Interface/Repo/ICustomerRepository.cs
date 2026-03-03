using Core.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interface.Repo
{
    public interface ICustomerRepository
    {
        Task<Customer?> GetByPhoneAsync(string phone);

        /// <summary>
        /// Finds a customer by phone (primary key search). If not found, creates a new one.
        /// Returns the resolved customer.
        /// </summary>
        Task<Customer> FindOrCreateAsync(string phone, string? fullName, string? email, CancellationToken ct = default);
    }
}
