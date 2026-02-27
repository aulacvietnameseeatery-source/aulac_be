using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO.Order
{
    public class CreateOrderRequest
    {
        public long? TableId { get; set; }
        public long? CustomerId { get; set; }
        public OrderSourceCode Source { get; set; } // DINE_IN | TAKEAWAY
        public List<CreateOrderItemDto> Items { get; set; } = new();
    }

    public class AddOrderItemsRequest
    {
        public List<CreateOrderItemDto> Items { get; set; } = new();
    }

    public class CreateOrderItemDto
    {
        public long DishId { get; set; }
        public int Quantity { get; set; }
        public string? Note { get; set; }
    }
}
