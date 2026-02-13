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
    }
}
