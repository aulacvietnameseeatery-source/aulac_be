using Core.DTO.Customer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interface.Service.Customer
{
    public interface ICustomerService
    {
        Task<CustomerDto?> GetByPhoneAsync(string phone);

        /// <summary>
        /// Looks up a customer by phone. If not found, creates a new record.
        /// Returns the customer_id to embed in the order.
        /// </summary>
        Task<long> FindOrCreateCustomerIdAsync(string phone, string? fullName, string? email, CancellationToken ct = default);
    }
}
