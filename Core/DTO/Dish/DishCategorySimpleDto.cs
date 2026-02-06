using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO.Dish
{
    public class DishCategorySimpleDto
    {
        public long CategoryId { get; set; }
        public string CategoryName { get; set; } = null!;
    }
}
