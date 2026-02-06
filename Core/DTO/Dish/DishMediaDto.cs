using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO.Dish
{
    public class DishMediaDto
    {
        public long MediaId { get; set; }
        public string Url { get; set; } = null!;
        public string MediaType { get; set; } = null!;
        public bool IsPrimary { get; set; }
    }

}
