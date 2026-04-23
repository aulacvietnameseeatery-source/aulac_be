using Core.DTO.Customer;
using Core.DTO.General;
using Core.Entity;
using Core.Extensions;
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
            var normalizedPhone = phone.NormalizePhoneNumber();
            if (string.IsNullOrWhiteSpace(normalizedPhone))
                return null;

            var customer = await _customerRepository.GetByPhoneAsync(normalizedPhone);

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
            var normalizedPhone = phone.NormalizePhoneNumber();
            var customer = await _customerRepository.FindOrCreateAsync(normalizedPhone, fullName, email, ct);
            return customer.CustomerId;
        }

        public async Task<long> ResolveCustomerAsync(
            OrderCustomerDto? customerDto,
            CancellationToken ct)
        {
            if (customerDto == null)
                return await GetGuestCustomerIdAsync(ct);

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
                var normalizedPhone = customerDto.Phone.NormalizePhoneNumber();
                var existing = await _customerRepository.GetByPhoneAsync(normalizedPhone);

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
                    Phone = normalizedPhone,
                    FullName = customerDto.FullName,
                    Email = customerDto.Email,
                    CreatedAt = DateTime.UtcNow,
                    LoyaltyPoints = 0,
                    IsMember = true
                };

                await _customerRepository.AddAsync(newCustomer, ct);

                return newCustomer.CustomerId;
            }

            return await GetGuestCustomerIdAsync(ct);
        }

        public async Task<CustomerDto> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken ct = default)
        {
            var normalizedPhone = request.Phone.NormalizePhoneNumber();

            var existing = await _customerRepository.GetByPhoneAsync(normalizedPhone);
            if (existing != null)
                throw new InvalidOperationException($"A customer with phone '{normalizedPhone}' already exists.");

            var customer = new Customer
            {
                Phone = normalizedPhone,
                FullName = string.IsNullOrWhiteSpace(request.FullName) ? null : request.FullName.Trim(),
                Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
                IsMember = request.IsMember,
                LoyaltyPoints = 0,
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

            var normalizedPhone = request.Phone.NormalizePhoneNumber();

            // Check phone uniqueness when changed
            if (customer.Phone != normalizedPhone)
            {
                var existing = await _customerRepository.GetByPhoneAsync(normalizedPhone);
                if (existing != null && existing.CustomerId != id)
                    throw new InvalidOperationException($"A customer with phone '{normalizedPhone}' already exists.");
            }

            customer.Phone = normalizedPhone;
            customer.FullName = string.IsNullOrWhiteSpace(request.FullName) ? null : request.FullName.Trim();
            customer.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
            customer.IsMember = request.IsMember;

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

        public Task<CustomerDetailDTO?> GetCustomerDetailAsync(
        long customerId,
        CancellationToken ct)
        {
            return _customerRepository.GetCustomerDetailAsync(customerId, ct);
        }

        public Task<PagedResultDTO<CustomerOrderDTO>> GetCustomerOrdersAsync(
            CustomerOrderQueryDTO query,
            CancellationToken ct)
        {
            return _customerRepository.GetCustomerOrdersAsync(query, ct);
        }

        public Task<CustomerOrderDetailDTO?> GetCustomerOrderDetailAsync(
            long customerId,
            long orderId,
            CancellationToken ct)
        {
            return _customerRepository.GetCustomerOrderDetailAsync(
                customerId,
                orderId,
                ct);
        }

        public async Task<long> GetGuestCustomerIdAsync(CancellationToken ct)
        {
            var guest = await _customerRepository.GetGuestCustomerAsync(ct);

            if (guest != null)
                return guest.CustomerId;

            var newGuest = new Customer
            {
                Phone = PhoneNumberExtensions.GuestPhoneSentinel,
                FullName = "Guest Customer",
                Email = null,
                IsMember = false,
                LoyaltyPoints = 0,
                CreatedAt = DateTime.UtcNow
            };

            await _customerRepository.AddAsync(newGuest, ct);

            return newGuest.CustomerId;
        }

        public async Task<List<CustomerDto>> SearchByPhoneAsync(
            string keyword,
            int limit,
            CancellationToken ct)
        {
            var customers = await _customerRepository.SearchByPhoneAsync(keyword, limit, ct);

            return customers.Select(customer => new CustomerDto
            {
                CustomerId = customer.CustomerId,
                FullName = customer.FullName,
                Phone = customer.Phone,
                Email = customer.Email,
                IsMember = customer.IsMember,
                LoyaltyPoints = customer.LoyaltyPoints,
                CreatedAt = customer.CreatedAt
            }).ToList();
        }
    }
}
