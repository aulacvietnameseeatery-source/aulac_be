using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO.Dish
{
    public class DishPosResponseDto
    {
        public long DishId { get; set; }
        public long CategoryId { get; set; }
        public decimal Price { get; set; }
        public bool? ChefRecommended { get; set; }
        public sbyte? DisplayOrder { get; set; }

        public Dictionary<string, DishI18nDto> I18n { get; set; } = new();
    }
}
