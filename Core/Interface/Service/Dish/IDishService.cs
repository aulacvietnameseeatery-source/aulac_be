using Core.DTO.Dish;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interface.Service.Dish
{
    public interface IDishService
    {
        Task<long> CreateDishAsync(
            CreateDishRequest request,
            IReadOnlyList<IFormFile> staticImages,
            IReadOnlyList<IFormFile> images360,
            CancellationToken ct
        );

    }
}
