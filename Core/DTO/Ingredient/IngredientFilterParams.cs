using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO.Ingredient
{
    public class IngredientFilterParams 
    {
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        public string? Search { get; set; }
        public uint? TypeLvId { get; set; }
        public bool? IsLowStock { get; set; } // Lọc cảnh báo hết hàng
    }
}
