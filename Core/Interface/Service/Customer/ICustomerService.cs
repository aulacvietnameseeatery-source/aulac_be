using Core.DTO.Customer;
using Core.DTO.General;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interface.Service.Customer
{
    public interface ICustomerService
    {
        Task<PagedResultDTO<CustomerListDTO>> GetCustomersAsync(CustomerListQueryDTO query, CancellationToken ct);
        Task<CustomerDto?> GetByPhoneAsync(string phone);

        Task<CustomerDto?> GetByIdAsync(long id, CancellationToken ct = default);

        /// <summary>
        /// Looks up a customer by phone. If not found, creates a new record.
        /// Returns the customer_id to embed in the order.
        /// </summary>
        Task<long> FindOrCreateCustomerIdAsync(string phone, string? fullName, string? email, CancellationToken ct = default);

        Task<long> ResolveCustomerAsync(OrderCustomerDto? customerDto, CancellationToken ct);

        Task<CustomerDetailDTO?> GetCustomerDetailAsync(long customerId, CancellationToken ct);

        Task<PagedResultDTO<CustomerOrderDTO>> GetCustomerOrdersAsync(CustomerOrderQueryDTO query, CancellationToken ct);

        Task<CustomerOrderDetailDTO?> GetCustomerOrderDetailAsync(long customerId, long orderId, CancellationToken ct);

        Task<CustomerDto> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken ct = default);
        Task<CustomerDto> UpdateCustomerAsync(long id, UpdateCustomerRequest request, CancellationToken ct = default);
        Task DeleteCustomerAsync(long id, CancellationToken ct = default);
    }
}
