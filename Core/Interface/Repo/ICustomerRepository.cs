using Core.DTO.Customer;
using Core.DTO.General;
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
        Task<PagedResultDTO<CustomerListDTO>> GetCustomersAsync(CustomerListQueryDTO query, CancellationToken ct = default);
        Task<Customer?> GetByIdAsync(long id, CancellationToken ct = default);
        Task<Customer?> GetByPhoneAsync(string phone);

        /// <summary>
        /// Finds a customer by phone (primary key search). If not found, creates a new one.
        /// Returns the resolved customer.
        /// </summary>
        Task<Customer> FindOrCreateAsync(string phone, string? fullName, string? email, CancellationToken ct = default);

        Task UpdateAsync(Customer customer, CancellationToken ct = default);
        Task AddAsync(Customer customer, CancellationToken ct = default);
    }
}
