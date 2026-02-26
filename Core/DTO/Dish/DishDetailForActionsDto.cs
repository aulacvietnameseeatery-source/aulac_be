using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO.Dish
{
    public class DishDetailForActionsDto
    {
        public long DishId { get; set; }
        public long CategoryId { get; set; }
        public Dictionary<string, string> CategoryName { get; set; } = new();
        public decimal Price { get; set; }
        public uint DishStatusLvId { get; set; }
        public Dictionary<string, string> DishStatus { get; set; } = new();
        public List<uint> TagIds { get; set; } = new();
        public List<TagMultiLangDto> Tags { get; set; } = new();

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

    public class TagMultiLangDto
    {
        public uint TagId { get; set; }

        public Dictionary<string, string> Names { get; set; } = new();
    }
}
