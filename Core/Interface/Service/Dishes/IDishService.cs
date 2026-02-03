using Core.DTO.Dish;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Entity;

namespace Core.Interface.Service.Dishes
{
    public interface IDishService
    {
        Task<(List<Dish> Items, int TotalCount)> GetAllDishesAsync(GetDishesRequest request);
    }
}
