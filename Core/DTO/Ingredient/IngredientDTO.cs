using Core.DTO.Supplier;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO.Ingredient
{
    public class IngredientDTO
    {
        public long IngredientId { get; set; }
        public string IngredientName { get; set; } = null!;
        public string Unit { get; set; } = null!;
        public uint? TypeLvId { get; set; }
        public string? TypeName { get; set; }
        public long? ImageId { get; set; }
        public string? ImageUrl { get; set; }

        // Stock Info
        public decimal QuantityOnHand { get; set; }
        public decimal MinStockLevel { get; set; }
        public DateTime? LastUpdatedAt { get; set; }

        // Suppliers Info
        public List<SupplierDto> Suppliers { get; set; } = new List<SupplierDto>();
    }
}
