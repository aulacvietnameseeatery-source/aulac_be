using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO.Dish
{
    public class DishManagementDto
    {
        public long DishId { get; set; }
        public string DishName { get; set; }
        public string CategoryName { get; set; }
        public decimal Price { get; set; }
        public string Status { get; set; }
        public long StatusId { get; set; }
        public bool IsOnline { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
