using Core.DTO.Dish;
using Core.Entity;
using Core.Interface.Repo;
using Core.Interface.Service.Dishes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Service
{
    public class DishService : IDishService
    {

        private readonly IDishRepository _dishRepository;
        public DishService(IDishRepository dishRepository)
        {
            _dishRepository = dishRepository;
        }

        public async Task<(List<Dish> Items, int TotalCount)> GetAllDishesAsync(GetDishesRequest request)
        {
            return await _dishRepository.GetDishesAsync(request);
        }
    }
}
