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
        public uint UnitLvId { get; set; }

        public uint? TypeLvId { get; set; }
        public long? ImageId { get; set; }
        public decimal MinStockLevel { get; set; }
        public List<long> SupplierIds { get; set; } = new List<long>();
    }
}
