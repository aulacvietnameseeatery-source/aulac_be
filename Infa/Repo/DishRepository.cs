using Core.DTO.Dish;
using Core.Entity;
using Core.Interface.Repo;
using Infa.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Infa.Repo
{
    public class DishRepository : IDishRepository
    {
        private readonly RestaurantMgmtContext _context;

        public DishRepository(RestaurantMgmtContext context)
        {
            _context = context;
        }

        public async Task<(List<Dish> Items, int TotalCount)> GetDishesAsync(GetDishesRequest request)
        {
            // Base query
            var dish = _context.Dishes
                .Include(d => d.Category)       // Take related Category
                .Include(d => d.DishStatusLv)   // Take related Status
                .AsNoTracking()                 // Make query read-only
                .AsQueryable();

            // Apply filters
            // Filter by category
            if (!string.IsNullOrWhiteSpace(request.Category) && request.Category != "All")
            {
                dish = dish.Where(d => d.Category.CategoryName == request.Category);
            }

            // Filter by search term
            if (!string.IsNullOrWhiteSpace(request.Status) && request.Status != "All")
            {
                // Filter by status
                dish = dish.Where(d => d.DishStatusLv.ValueCode == request.Status);
            }

            // Sorting
            if(string .IsNullOrWhiteSpace( request.SortBy))
            {
                dish = dish.OrderBy(d => d.DishId); // Default sort by DishId
            }
            else
            {
                switch (request.SortBy.ToLower())
                {
                    case "price":
                        dish = request.IsDescending
                            ? dish.OrderByDescending(d => d.Price)
                            : dish.OrderBy(d => d.Price);
                        break;
                    case "name":
                        dish = request.IsDescending
                            ? dish.OrderByDescending(d => d.DishName)
                            : dish.OrderBy(d => d.DishName);
                        break;
                    default:
                        dish = dish.OrderByDescending(d => d.CreatedAt);
                        break;
                }
            }

            // Get total count before pagination
            var totalCount = await dish.CountAsync();
            // Apply pagination
            var items = await dish
                .Skip((request.PageIndex - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return (items, totalCount);
        }
    }
}
