using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO.Dish
{
    public class DishDetailDto
    {
        public long DishId { get; set; }
        public long CategoryId { get; set; }
        public string CategoryName { get; set; } = null!;
        public decimal Price { get; set; }
        public uint DishStatusLvId { get; set; }
        public string DishStatus { get; set; } = null!;
        public uint TagId { get; set; }
        public string TagName { get; set; } = null!;

        public bool IsOnline { get; set; }
        public bool ChefRecommended { get; set; }

        public int? DisplayOrder { get; set; }
        public int? Calories { get; set; }
        public int? PrepTimeMinutes { get; set; }
        public int? CookTimeMinutes { get; set; }

        public Dictionary<string, DishI18nDto> I18n { get; set; }
            = new();

        public List<DishMediaDto> Media { get; set; } = new();
    }

}
