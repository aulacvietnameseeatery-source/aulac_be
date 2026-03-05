using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO.Ingredient
{
    public class AdjustStockRequest
    {
        public decimal Quantity { get; set; } 
        public string Note { get; set; } = null!; // Lý do 
    }
}
