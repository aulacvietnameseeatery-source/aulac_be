using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO.Dish
{
    public class DishDisplayDto
    {
        public long DishId { get; set; }

        // Thay string bằng I18nTextDto
        public I18nTextDto DishName { get; set; } = new I18nTextDto();

        public decimal Price { get; set; }

        // Thay string bằng I18nTextDto
        public I18nTextDto CategoryName { get; set; } = new I18nTextDto();

        // Đổi tên Tagline thành Description cho chuẩn với database và thay bằng I18nTextDto
        public I18nTextDto? Description { get; set; }

        public string? ImageUrl { get; set; }
        public bool IsChefRecommended { get; set; }
    }
}
