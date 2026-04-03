using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO.Dish
{
    public class DishManagementDto
    {
        public long DishId { get; set; }

        public string DishName { get; set; } = string.Empty;

        public string CategoryName { get; set; } = string.Empty;

        public I18nTextDto NameI18n { get; set; } = new();
        public I18nTextDto DescriptionI18n { get; set; } = new();
        public I18nTextDto CategoryNameI18n { get; set; } = new();

        public decimal Price { get; set; }
        public string Status { get; set; } = string.Empty;
        public long StatusId { get; set; }
        public bool IsOnline { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}