using Core.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interface.Repo
{
    public interface IDishRepository
    {
        Task AddAsync(Dish dish, CancellationToken ct);
        Task<Dish?> FindByIdAsync(long id, CancellationToken ct);
    }
}
