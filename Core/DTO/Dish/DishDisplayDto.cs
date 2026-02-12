using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO.Dish
{
    public class DishDisplayDto
    {
        public long DishId { get; set; }
        public string DishName { get; set; }
        public decimal Price { get; set; }
        public string CategoryName { get; set; }
        public string? Tagline { get; set; } 
        public string? ImageUrl { get; set; } 
        public bool IsChefRecommended { get; set; }
    }
}
