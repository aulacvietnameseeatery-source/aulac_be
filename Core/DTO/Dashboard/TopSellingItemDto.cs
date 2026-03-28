using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO.Dashboard
{
    public class TopSellingItemDto
    {
        public long DishId { get; set; }
        public string DishName { get; set; } = string.Empty;
        public int TotalQuantity { get; set; }
        public string? ImageUrl { get; set; }
    }
}
