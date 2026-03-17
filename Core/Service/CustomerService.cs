using Core.DTO.Customer;
using Core.DTO.General;
using Core.Entity;
using Core.Exceptions;
using Core.Interface.Repo;
using Core.Interface.Service.Customer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Service
{
    public class CustomerService : ICustomerService
    {
        private const long GuestCustomerId = 68;

        private readonly ICustomerRepository _customerRepository;

        public CustomerService(ICustomerRepository customerRepository)
        {
            _customerRepository = customerRepository;
        }

        public Task<PagedResultDTO<CustomerListDTO>> GetCustomersAsync(
            CustomerListQueryDTO query,
            CancellationToken ct)
        {
            return _customerRepository.GetCustomersAsync(query, ct);
        }

        public async Task<CustomerDto?> GetByPhoneAsync(string phone)
        {
            var customer = await _customerRepository.GetByPhoneAsync(phone);

            if (customer == null)
                return null;

            return new CustomerDto
            {
                CustomerId = customer.CustomerId,
                FullName = customer.FullName,
                Phone = customer.Phone,
                Email = customer.Email,
                IsMember = customer.IsMember,
                LoyaltyPoints = customer.LoyaltyPoints,
                CreatedAt = customer.CreatedAt
            };
        }

        public async Task<CustomerDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        {
            var customer = await _customerRepository.GetByIdAsync(id, cancellationToken);

            if (customer == null)
                return null;

            return new CustomerDto
            {
                CustomerId = customer.CustomerId,
                FullName = customer.FullName,
                Phone = customer.Phone,
                Email = customer.Email,
                IsMember = customer.IsMember,
                LoyaltyPoints = customer.LoyaltyPoints,
                CreatedAt = customer.CreatedAt
            };
        }

        public async Task<long> FindOrCreateCustomerIdAsync(string phone, string? fullName, string? email, CancellationToken ct = default)
        {
            var customer = await _customerRepository.FindOrCreateAsync(phone, fullName, email, ct);
            return customer.CustomerId;
        }

        public async Task<long> ResolveCustomerAsync(
            OrderCustomerDto? customerDto,
            CancellationToken ct)
        {
            if (customerDto == null)
                return GuestCustomerId;

            // CASE 1: Update existing customer by ID
            if (customerDto.CustomerId.HasValue)
            {
                var customer = await _customerRepository.GetByIdAsync(customerDto.CustomerId.Value, ct)
                    ?? throw new NotFoundException("Customer not found.");

                bool updated = false;

                if (!string.IsNullOrWhiteSpace(customerDto.FullName) &&
                    customer.FullName != customerDto.FullName)
                {
                    customer.FullName = customerDto.FullName;
                    updated = true;
                }

                if (!string.IsNullOrWhiteSpace(customerDto.Email) &&
                    customer.Email != customerDto.Email)
                {
                    customer.Email = customerDto.Email;
                    updated = true;
                }

                if (updated)
                    await _customerRepository.UpdateAsync(customer, ct);

                return customer.CustomerId;
            }

            // CASE 2: Check by phone
            if (!string.IsNullOrWhiteSpace(customerDto.Phone))
            {
                var existing = await _customerRepository.GetByPhoneAsync(customerDto.Phone);

                if (existing != null)
                {
                    bool updated = false;

                    if (!string.IsNullOrWhiteSpace(customerDto.FullName) &&
                        existing.FullName != customerDto.FullName)
                    {
                        existing.FullName = customerDto.FullName;
                        updated = true;
                    }

                    if (!string.IsNullOrWhiteSpace(customerDto.Email) &&
                        existing.Email != customerDto.Email)
                    {
                        existing.Email = customerDto.Email;
                        updated = true;
                    }

                    if (updated)
                        await _customerRepository.UpdateAsync(existing, ct);

                    return existing.CustomerId;
                }

                // Create new customer
                var newCustomer = new Customer
                {
                    Phone = customerDto.Phone,
                    FullName = customerDto.FullName,
                    Email = customerDto.Email,
                    CreatedAt = DateTime.UtcNow,
                    LoyaltyPoints = 0,
                    IsMember = false
                };

                await _customerRepository.AddAsync(newCustomer, ct);

                return newCustomer.CustomerId;
            }

            return GuestCustomerId;
        }

        public async Task<CustomerDto> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken ct = default)
        {
            var trimmedPhone = request.Phone.Trim();

            var existing = await _customerRepository.GetByPhoneAsync(trimmedPhone);
            if (existing != null)
                throw new InvalidOperationException($"A customer with phone '{trimmedPhone}' already exists.");

            var customer = new Customer
            {
                Phone = trimmedPhone,
                FullName = string.IsNullOrWhiteSpace(request.FullName) ? null : request.FullName.Trim(),
                Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
                IsMember = request.IsMember,
                LoyaltyPoints = request.LoyaltyPoints,
                CreatedAt = DateTime.UtcNow
            };

            await _customerRepository.AddAsync(customer, ct);

            return new CustomerDto
            {
                CustomerId = customer.CustomerId,
                FullName = customer.FullName,
                Phone = customer.Phone,
                Email = customer.Email,
                IsMember = customer.IsMember,
                LoyaltyPoints = customer.LoyaltyPoints,
                CreatedAt = customer.CreatedAt
            };
        }

        public async Task<CustomerDto> UpdateCustomerAsync(long id, UpdateCustomerRequest request, CancellationToken ct = default)
        {
            var customer = await _customerRepository.GetByIdAsync(id, ct)
                ?? throw new KeyNotFoundException($"Customer with ID {id} was not found.");

            var trimmedPhone = request.Phone.Trim();

            // Check phone uniqueness when changed
            if (customer.Phone != trimmedPhone)
            {
                var existing = await _customerRepository.GetByPhoneAsync(trimmedPhone);
                if (existing != null && existing.CustomerId != id)
                    throw new InvalidOperationException($"A customer with phone '{trimmedPhone}' already exists.");
            }

            customer.Phone = trimmedPhone;
            customer.FullName = string.IsNullOrWhiteSpace(request.FullName) ? null : request.FullName.Trim();
            customer.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
            customer.IsMember = request.IsMember;
            customer.LoyaltyPoints = request.LoyaltyPoints;

            await _customerRepository.UpdateAsync(customer, ct);

            return new CustomerDto
            {
                CustomerId = customer.CustomerId,
                FullName = customer.FullName,
                Phone = customer.Phone,
                Email = customer.Email,
                IsMember = customer.IsMember,
                LoyaltyPoints = customer.LoyaltyPoints,
                CreatedAt = customer.CreatedAt
            };
        }

        public async Task DeleteCustomerAsync(long id, CancellationToken ct = default)
        {
            var customer = await _customerRepository.GetByIdAsync(id, ct)
                ?? throw new KeyNotFoundException($"Customer with ID {id} was not found.");

            var hasDependencies = await _customerRepository.HasOrdersOrReservationsAsync(id, ct);
            if (hasDependencies)
                throw new InvalidOperationException("Cannot delete a customer who has existing orders or reservations.");

            await _customerRepository.DeleteAsync(customer, ct);
        }
    }
}
