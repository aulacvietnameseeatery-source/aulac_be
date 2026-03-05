using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO.Ingredient
{
    public class SaveIngredientRequest
    {
        public string IngredientName { get; set; } = null!;
        public string Unit { get; set; } = null!;
        public uint? TypeLvId { get; set; }
        public long? ImageId { get; set; }
        public decimal MinStockLevel { get; set; } // Set mức cảnh báo
        public List<long> SupplierIds { get; set; } = new List<long>(); 
    }
}
