using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO.Dish
{
    public class TranslateDishResponse
    {
        public Dictionary<string, DishI18nDto> Translations { get; set; } = new();
    }
}
