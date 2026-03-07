using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO.Ingredient
{
    public class StockHistoryDto
    {
        public long TransactionItemId { get; set; }
        public decimal QuantityChanged { get; set; }
        public string Note { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }
}
