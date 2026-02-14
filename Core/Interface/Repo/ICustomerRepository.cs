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
    }
}
