using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO.Dish
{
    public class TranslateDishRequest
    {
        public string SourceLang { get; set; } = null!;
        public DishI18nDto Data { get; set; } = null!;
    }
}
