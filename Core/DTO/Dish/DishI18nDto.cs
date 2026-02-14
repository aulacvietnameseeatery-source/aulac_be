using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO.Dish
{
    public class DishI18nDto
    {
        public string DishName { get; set; } = null!;
        public string? Description { get; set; }
        public string? Slogan { get; set; }
        public string? Note { get; set; }
        public string? ShortDescription { get; set; }
    }

    public class DishI18nTextIds
    {
        public long DishNameTextId { get; set; }
        public long? DescriptionTextId { get; set; }
        public long? SloganTextId { get; set; }
        public long? NoteTextId { get; set; }
        public long? ShortDescriptionTextId { get; set; }
    }
}
