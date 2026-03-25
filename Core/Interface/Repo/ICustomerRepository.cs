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

        Task<CustomerDetailDTO?> GetCustomerDetailAsync(long customerId, CancellationToken ct);

        Task<PagedResultDTO<CustomerOrderDTO>> GetCustomerOrdersAsync(CustomerOrderQueryDTO query, CancellationToken ct);

        Task<CustomerOrderDetailDTO?> GetCustomerOrderDetailAsync(long customerId, long orderId, CancellationToken ct);
        Task DeleteAsync(Customer customer, CancellationToken ct = default);
        Task<bool> HasOrdersOrReservationsAsync(long customerId, CancellationToken ct = default);
        Task<Customer?> GetGuestCustomerAsync(CancellationToken ct);
        Task<List<Customer>> SearchByPhoneAsync(string keyword, int limit, CancellationToken ct);
    }
}
