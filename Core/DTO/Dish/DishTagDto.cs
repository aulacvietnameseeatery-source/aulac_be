using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO.Dish
{
    public class DishTagDto
    {
        public uint TagId { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
    }
}
