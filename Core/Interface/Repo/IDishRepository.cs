using Core.DTO.Dish;
using Core.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interface.Repo;

public interface IDishRepository
{
    Task<Dish?> GetDishByIdAsync(long dishId, CancellationToken cancellationToken = default);
        Task<(List<Dish> Items, int TotalCount)> GetDishesAsync(GetDishesRequest request);
    }
}
