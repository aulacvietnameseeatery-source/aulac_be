using Core.DTO.Dish;
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
        Task<(List<Dish> Items, int TotalCount)> GetDishesAsync(GetDishesRequest request);
    }
}
