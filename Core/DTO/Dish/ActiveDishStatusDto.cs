using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO.Dish
{
    public class ActiveDishStatusDto
    {
        public uint DishStatusLvId { get; set; }
        public string ValueName { get; set; } = null!;
        public string ValueCode { get; set; } = null!;
    }

    public class TranslationDto
    {
        public string LangCode { get; set; } = null!;
        public string Text { get; set; } = null!;
    }
}
